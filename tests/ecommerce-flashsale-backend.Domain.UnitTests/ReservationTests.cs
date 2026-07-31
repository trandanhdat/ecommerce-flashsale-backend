using System;
using System.Linq;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.Reservations.Events;
using FlashSale.Domain.Reservations.Exceptions;
using FluentAssertions;
using Xunit;

namespace ecommerce_flashsale_backend.Domain.UnitTests.Reservations
{
    public class ReservationTests
    {
        [Fact]
        public void Expire_ShouldSetStatusToExpired_WhenCurrentlyHolding()
        {
            // Arrange
            var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow.AddMinutes(5));
            reservation.ClearDomainEvents(); // Clear creation event

            // Act
            reservation.Expire();

            // Assert
            reservation.Status.Should().Be(ReservationStatus.Expired);
            reservation.DomainEvents.Should().ContainSingle(e => e is ReservationExpiredEvent);
        }

        [Fact]
        public void Expire_ShouldDoNothing_WhenAlreadyExpired()
        {
            // Arrange
            var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow.AddMinutes(5));
            reservation.Expire();
            reservation.ClearDomainEvents();

            // Act
            reservation.Expire();

            // Assert
            reservation.Status.Should().Be(ReservationStatus.Expired);
            reservation.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void ConvertToOrder_ShouldSetStatusToConverted_WhenValid()
        {
            // Arrange
            var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow.AddMinutes(5));
            reservation.ClearDomainEvents();
            var orderId = Guid.NewGuid();

            // Act
            reservation.ConvertToOrder(orderId);

            // Assert
            reservation.Status.Should().Be(ReservationStatus.Converted);
            reservation.OrderId.Should().Be(orderId);
            reservation.DomainEvents.Should().ContainSingle(e => e is ReservationConvertedEvent);
        }

        [Fact]
        public void ConvertToOrder_ShouldThrowException_WhenAlreadyExpired()
        {
            // Arrange
            var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow.AddMinutes(5));
            reservation.Expire();
            var orderId = Guid.NewGuid();

            // Act & Assert
            var action = () => reservation.ConvertToOrder(orderId);
            action.Should().Throw<ReservationAlreadyExpiredException>();
        }
    }
}
