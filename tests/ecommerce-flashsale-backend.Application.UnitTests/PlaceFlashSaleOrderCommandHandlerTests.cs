using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Catalog.ValueObjects;
using FlashSale.Application.Common.Enums;

namespace ecommerce_flashsale_backend.Application.UnitTests
{
    public class PlaceFlashSaleOrderCommandHandlerTests
    {
        private readonly Mock<IFlashSaleRepository> _mockFlashSaleRepo;
        private readonly Mock<IReservationRepository> _mockReservationRepo;
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<IFlashSaleStockCache> _mockStockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDistributedLockService> _mockLockService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<PlaceFlashSaleOrderCommandHandler>> _mockLogger;

        private readonly PlaceFlashSaleOrderCommandHandler _handler;

        public PlaceFlashSaleOrderCommandHandlerTests()
        {
            _mockFlashSaleRepo = new Mock<IFlashSaleRepository>();
            _mockReservationRepo = new Mock<IReservationRepository>();
            _mockOrderRepo = new Mock<IOrderRepository>();
            _mockStockCache = new Mock<IFlashSaleStockCache>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLockService = new Mock<IDistributedLockService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<PlaceFlashSaleOrderCommandHandler>>();

            // Setup Configuration cho HoldingMinutes
            _mockConfiguration.Setup(c => c["FlashSale:ReservationHoldingMinutes"]).Returns("5");

            _handler = new PlaceFlashSaleOrderCommandHandler(
                _mockFlashSaleRepo.Object,
                _mockReservationRepo.Object,
                _mockOrderRepo.Object,
                _mockStockCache.Object,
                _mockUnitOfWork.Object,
                _mockEventPublisher.Object,
                _mockCurrentUserService.Object,
                _mockLockService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenAllConditionsAreMet()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var flashSaleItemId = Guid.NewGuid();
            var quantity = 1;
            var command = new PlaceFlashSaleOrderCommand(flashSaleItemId, quantity);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            
            // Giả lập lấy Lock thành công
            _mockLockService.Setup(l => l.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Giả lập Item hợp lệ và Active
            var flashSaleItem = new FlashSaleItem(Guid.NewGuid(), Guid.NewGuid(), new Money(1000m, "VND"), 100, 1);
            _mockFlashSaleRepo.Setup(r => r.GetActiveItemByIdAsync(flashSaleItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(flashSaleItem);

            // Giả lập chưa có Reservation nào pending
            _mockReservationRepo.Setup(r => r.GetHoldingByUserAndItemAsync(userId, flashSaleItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation)null);

            // Giả lập trừ kho Redis thành công
            _mockStockCache.Setup(s => s.TryDecrementStockAsync(flashSaleItemId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(StockDecrementResult.Success);

            // Giả lập save DB thành công
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.OrderId.Should().NotBeNull();
            result.ReservationId.Should().NotBeNull();
            
            // Đảm bảo Lock đã được release
            _mockLockService.Verify(l => l.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            // Đảm bảo Order và Reservation được Add vào Repo
            _mockReservationRepo.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Once);
            _mockOrderRepo.Verify(o => o.Add(It.IsAny<Order>()), Times.Once);
            
            // Đảm bảo Event Publish được gọi
            _mockEventPublisher.Verify(e => e.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRollbackRedis_WhenDbSaveFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var flashSaleItemId = Guid.NewGuid();
            var quantity = 1;
            var command = new PlaceFlashSaleOrderCommand(flashSaleItemId, quantity);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockLockService.Setup(l => l.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var flashSaleItem = new FlashSaleItem(Guid.NewGuid(), Guid.NewGuid(), new Money(1000m, "VND"), 100, 1);
            _mockFlashSaleRepo.Setup(r => r.GetActiveItemByIdAsync(flashSaleItemId, It.IsAny<CancellationToken>())).ReturnsAsync(flashSaleItem);
            
            _mockReservationRepo.Setup(r => r.GetHoldingByUserAndItemAsync(userId, flashSaleItemId, It.IsAny<CancellationToken>())).ReturnsAsync((Reservation)null);
            
            _mockStockCache.Setup(s => s.TryDecrementStockAsync(flashSaleItemId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(StockDecrementResult.Success); // Trừ Redis thành công

            // Nhưng Save DB bị văng lỗi!
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection lost"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Lỗi hệ thống khi lưu đơn hàng, đã hoàn lại kho");

            // QUAN TRỌNG NHẤT: Đảm bảo hàm hoàn lại kho (IncrementStockAsync) PHẢI ĐƯỢC GỌI
            _mockStockCache.Verify(s => s.IncrementStockAsync(flashSaleItemId, quantity, It.IsAny<CancellationToken>()), Times.Once);

            // Đảm bảo Event Publish KHÔNG ĐƯỢC GỌI vì đơn hàng thất bại
            _mockEventPublisher.Verify(e => e.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenUserNotLoggedIn()
        {
            // Arrange
            var command = new PlaceFlashSaleOrderCommand(Guid.NewGuid(), 1);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Empty); // Không đăng nhập

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User chưa đăng nhập.");
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenRateLimited()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new PlaceFlashSaleOrderCommand(Guid.NewGuid(), 1);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            
            // Giả lập khóa đang bị người khác (hoặc chính user đó) giữ
            _mockLockService.Setup(l => l.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); 

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Yêu cầu đang được xử lý, vui lòng không bấm liên tục");
            
            // Đảm bảo code DỪNG LẠI ngay và KHÔNG BAO GIỜ chạm vào Database
            _mockFlashSaleRepo.Verify(r => r.GetActiveItemByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenOutOfStock()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var flashSaleItemId = Guid.NewGuid();
            var quantity = 1;
            var command = new PlaceFlashSaleOrderCommand(flashSaleItemId, quantity);

            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockLockService.Setup(l => l.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var flashSaleItem = new FlashSaleItem(Guid.NewGuid(), Guid.NewGuid(), new Money(1000m, "VND"), 100, 1);
            _mockFlashSaleRepo.Setup(r => r.GetActiveItemByIdAsync(flashSaleItemId, It.IsAny<CancellationToken>())).ReturnsAsync(flashSaleItem);
            
            _mockReservationRepo.Setup(r => r.GetHoldingByUserAndItemAsync(userId, flashSaleItemId, It.IsAny<CancellationToken>())).ReturnsAsync((Reservation)null);
            
            // Giả lập Redis báo Hết Hàng!
            _mockStockCache.Setup(s => s.TryDecrementStockAsync(flashSaleItemId, quantity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(StockDecrementResult.InsufficientStock);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(PlaceFlashSaleOrderErrorCode.InsufficientStock);
            result.ErrorMessage.Should().Be("Sản phẩm đã hết hàng.");
            // Đảm bảo không có dòng dữ liệu Order nào được Insert vào DB
            _mockOrderRepo.Verify(o => o.Add(It.IsAny<Order>()), Times.Never);
        }
    }
}
