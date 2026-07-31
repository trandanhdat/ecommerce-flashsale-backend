using System;
using System.Linq;
using FlashSale.Domain.Catalog.ValueObjects;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.FlashSales.Events;
using FlashSale.Domain.FlashSales.Exceptions;
using FluentAssertions;
using Xunit;

namespace ecommerce_flashsale_backend.Domain.UnitTests.FlashSales
{
    public class FlashSaleItemTests
    {
        [Fact]
        public void ReserveStock_ShouldIncreaseReservedCount_WhenStockIsAvailable()
        {
            // Arrange
            var flashSaleId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var salePrice = new Money(100000m);
            var saleStock = 10;
            var maxPerUser = 2;
            var item = new FlashSaleItem(flashSaleId, productId, salePrice, saleStock, maxPerUser);

            // Act
            item.ReserveStock(2);

            // Assert
            item.ReservedCount.Should().Be(2);
            item.DomainEvents.Should().ContainSingle(e => e is FlashSaleItemStockDecrementedEvent);
        }

        [Fact]
        public void ReserveStock_ShouldThrowException_WhenStockIsExceeded()
        {
            // Arrange
            var flashSaleId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var salePrice = new Money(100000m);
            var saleStock = 5;
            var maxPerUser = 2;
            var item = new FlashSaleItem(flashSaleId, productId, salePrice, saleStock, maxPerUser);

            // Act & Assert
            var action = () => item.ReserveStock(6);
            action.Should().Throw<FlashSaleStockExceededException>();
        }

        [Fact]
        public void ReserveStock_ShouldAddSoldOutEvent_WhenStockExactlyDepleted()
        {
            // Arrange
            var flashSaleId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var salePrice = new Money(100000m);
            var saleStock = 3;
            var maxPerUser = 3;
            var item = new FlashSaleItem(flashSaleId, productId, salePrice, saleStock, maxPerUser);

            // Act
            item.ReserveStock(3);

            // Assert
            item.ReservedCount.Should().Be(3);
            item.DomainEvents.Should().Contain(e => e is FlashSaleItemSoldOutEvent);
            item.DomainEvents.Should().Contain(e => e is FlashSaleItemStockDecrementedEvent);
        }

        [Fact]
        public void UpdateSoldCount_ShouldUpdate_WhenValid()
        {
            // Arrange
            var item = new FlashSaleItem(Guid.NewGuid(), Guid.NewGuid(), new Money(100000m), 10, 2);

            // Act
            item.UpdateSoldCount(5);

            // Assert
            item.SoldCount.Should().Be(5);
        }
    }
}
