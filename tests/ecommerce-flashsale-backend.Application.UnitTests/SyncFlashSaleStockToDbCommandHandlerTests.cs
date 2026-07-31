using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.SyncFlashSaleStockToDb;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;

namespace ecommerce_flashsale_backend.Application.UnitTests
{
    public class SyncFlashSaleStockToDbCommandHandlerTests
    {
        private readonly Mock<IFlashSaleRepository> _mockFlashSaleRepo;
        private readonly Mock<IFlashSaleStockCache> _mockStockCache;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<SyncFlashSaleStockToDbCommandHandler>> _mockLogger;
        private readonly SyncFlashSaleStockToDbCommandHandler _handler;

        public SyncFlashSaleStockToDbCommandHandlerTests()
        {
            _mockFlashSaleRepo = new Mock<IFlashSaleRepository>();
            _mockStockCache = new Mock<IFlashSaleStockCache>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<SyncFlashSaleStockToDbCommandHandler>>();

            _handler = new SyncFlashSaleStockToDbCommandHandler(
                _mockFlashSaleRepo.Object,
                _mockStockCache.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldDoNothing_WhenNoActiveFlashSalesFound()
        {
            // Arrange
            var command = new SyncFlashSaleStockToDbCommand();
            
            _mockFlashSaleRepo.Setup(r => r.GetActiveWithItemsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FlashSale.Domain.FlashSales.FlashSale>());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockStockCache.Verify(s => s.GetCurrentStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldSyncSoldCount_WhenStockIsAvailableInRedis()
        {
            // Arrange
            var command = new SyncFlashSaleStockToDbCommand();
            
            var flashSale = new FlashSale.Domain.FlashSales.FlashSale(
                "Test Sale", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            
            // Item có tổng kho (SaleStock) là 100
            var item1 = new FlashSaleItem(flashSale.Id, Guid.NewGuid(), null!, 100, 1);
            var item2 = new FlashSaleItem(flashSale.Id, Guid.NewGuid(), null!, 50, 1);
            
            flashSale.Items.Add(item1);
            flashSale.Items.Add(item2);

            var activeSales = new List<FlashSale.Domain.FlashSales.FlashSale> { flashSale };
            _mockFlashSaleRepo.Setup(r => r.GetActiveWithItemsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeSales);

            // Redis trả về số dư kho hiện tại là 30 cho Item1 (Tức là đã bán 100 - 30 = 70)
            _mockStockCache.Setup(s => s.GetCurrentStockAsync(item1.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(30);
                
            // Redis trả về số dư kho hiện tại là 0 cho Item2 (Tức là đã bán 50 - 0 = 50)
            _mockStockCache.Setup(s => s.GetCurrentStockAsync(item2.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            item1.SoldCount.Should().Be(70);
            item2.SoldCount.Should().Be(50);
            
            // Phải gọi SaveChanges đúng 2 lần (lưu từng phần cho từng Item để bắt Concurrency Exception)
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldContinueToNextItem_WhenDbUpdateConcurrencyExceptionOccurs()
        {
            // Arrange
            var command = new SyncFlashSaleStockToDbCommand();
            
            var flashSale = new FlashSale.Domain.FlashSales.FlashSale(
                "Test Sale", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            
            var item1 = new FlashSaleItem(flashSale.Id, Guid.NewGuid(), null!, 100, 1);
            var item2 = new FlashSaleItem(flashSale.Id, Guid.NewGuid(), null!, 100, 1);
            flashSale.Items.Add(item1);
            flashSale.Items.Add(item2);

            _mockFlashSaleRepo.Setup(r => r.GetActiveWithItemsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FlashSale.Domain.FlashSales.FlashSale> { flashSale });

            // Redis trả về bình thường
            _mockStockCache.Setup(s => s.GetCurrentStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(80); // Đã bán 20

            // Giả lập item 1 bị lỗi Concurrency khi Save DB
            _mockUnitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("Conflict")) // Lỗi lần 1
                .ReturnsAsync(1); // Lần 2 (item 2) bình thường

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Dù item 1 bị văng lỗi (catch bên trong), hàm vẫn phải đi tiếp và xử lý item 2, gọi đủ 2 lần Save
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            
            // Cả 2 item đều được gán SoldCount = 20, nhưng chỉ có thằng thứ 2 thực sự lưu thành công xuống DB (theo setup của UnitOfWork)
            item1.SoldCount.Should().Be(20); 
            item2.SoldCount.Should().Be(20);
        }
    }
}
