using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using MediatR;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.ExpireReservations;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using FlashSale.Application.Common.Enums;

namespace ecommerce_flashsale_backend.Application.UnitTests
{
    public class ExpireReservationsCommandHandlerTests
    {
        private readonly Mock<IReservationRepository> _mockReservationRepo;
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<IFlashSaleStockCache> _mockStockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<ExpireReservationsCommandHandler>> _mockLogger;

        private readonly ExpireReservationsCommandHandler _handler;

        public ExpireReservationsCommandHandlerTests()
        {
            _mockReservationRepo = new Mock<IReservationRepository>();
            _mockOrderRepo = new Mock<IOrderRepository>();
            _mockStockCache = new Mock<IFlashSaleStockCache>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<ExpireReservationsCommandHandler>>();

            _handler = new ExpireReservationsCommandHandler(
                _mockReservationRepo.Object,
                _mockOrderRepo.Object,
                _mockStockCache.Object,
                _mockUnitOfWork.Object,
                _mockMediator.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldDoNothing_WhenNoExpiredReservationsFound()
        {
            // Arrange
            var command = new ExpireReservationsCommand();
            
            // Giả lập DB trả về danh sách rỗng (không có đơn nào quá hạn)
            _mockReservationRepo.Setup(r => r.GetExpiredHoldingsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Reservation>());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Đảm bảo tuyệt đối không gọi thêm bất kỳ truy vấn nào xuống Database hay Redis
            _mockOrderRepo.Verify(o => o.GetPendingOrdersByReservationIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockStockCache.Verify(s => s.IncrementStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldExpireReservations_CancelOrders_AndIncrementStock_WhenExpiredReservationsExist()
        {
            // Arrange
            var command = new ExpireReservationsCommand();
            var userId = Guid.NewGuid();
            var flashSaleItemId1 = Guid.NewGuid();
            var flashSaleItemId2 = Guid.NewGuid();
            
            // Tạo 2 cái Reservation đã hết hạn
            var reservation1 = new Reservation(flashSaleItemId1, userId, 2, DateTime.UtcNow.AddMinutes(-10));
            var reservation2 = new Reservation(flashSaleItemId2, userId, 1, DateTime.UtcNow.AddMinutes(-5));
            var expiredReservations = new List<Reservation> { reservation1, reservation2 };

            _mockReservationRepo.Setup(r => r.GetExpiredHoldingsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredReservations);

            // Tạo 1 Order liên kết với Reservation1 (Giả sử Reservation2 chưa kịp tạo Order hoặc Order đã lỗi)
            var order1 = new Order(userId, OrderType.FlashSale, "N/A", "N/A", "N/A", DateTime.UtcNow, reservation1.Id);
            var relatedOrders = new List<Order> { order1 };

            _mockOrderRepo.Setup(o => o.GetPendingOrdersByReservationIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(relatedOrders);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // 1. Kiểm tra trạng thái của Reservation đã chuyển sang Expired chưa
            reservation1.Status.Should().Be(ReservationStatus.Expired);
            reservation2.Status.Should().Be(ReservationStatus.Expired);

            // 2. Kiểm tra trạng thái của Order đã chuyển sang Cancelled chưa
            order1.Status.Should().Be(OrderStatus.Cancelled);

            // 3. Đảm bảo Redis được gọi hoàn kho (IncrementStockAsync) CHÍNH XÁC 2 lần (cho 2 item)
            _mockStockCache.Verify(s => s.IncrementStockAsync(flashSaleItemId1, 2, It.IsAny<CancellationToken>()), Times.Once);
            _mockStockCache.Verify(s => s.IncrementStockAsync(flashSaleItemId2, 1, It.IsAny<CancellationToken>()), Times.Once);

            // 4. Đảm bảo DB được Save đúng 1 lần cuối cùng cho cả cục (Batch)
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
