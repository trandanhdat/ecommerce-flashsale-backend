using System;
using System.Linq;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Orders.Events;
using FlashSale.Domain.Orders.Exceptions;
using FluentAssertions;
using Xunit;

namespace ecommerce_flashsale_backend.Domain.UnitTests.Orders
{
    public class OrderTests
    {
        [Fact]
        public void AddOrderItem_ShouldCalculateTotalAmountCorrectly()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), OrderType.Normal, "John Doe", "0123456789", "Hanoi", DateTime.UtcNow.AddMinutes(30));
            
            // Act
            order.AddOrderItem(Guid.NewGuid(), 100000m, 2);
            order.AddOrderItem(Guid.NewGuid(), 50000m, 1);

            // Assert
            order.TotalAmount.Should().Be(250000m);
            order.OrderItems.Should().HaveCount(2);
        }

        [Fact]
        public void Confirm_ShouldSetStatusToConfirmed_WhenPending()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), OrderType.Normal, "John Doe", "0123456789", "Hanoi", DateTime.UtcNow.AddMinutes(30));
            order.ClearDomainEvents();

            // Act
            order.Confirm();

            // Assert
            order.Status.Should().Be(OrderStatus.Confirmed);
            order.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent);
        }

        [Fact]
        public void Cancel_ShouldThrowException_WhenOrderIsConfirmed()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), OrderType.Normal, "John Doe", "0123456789", "Hanoi", DateTime.UtcNow.AddMinutes(30));
            order.Confirm();

            // Act & Assert
            var action = () => order.Cancel();
            action.Should().Throw<OrderCannotBeCancelledException>();
        }

        [Fact]
        public void Cancel_ShouldSetStatusToCancelled_WhenPending()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), OrderType.Normal, "John Doe", "0123456789", "Hanoi", DateTime.UtcNow.AddMinutes(30));
            order.ClearDomainEvents();

            // Act
            order.Cancel();

            // Assert
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledEvent);
        }
    }
}
