using System;
using System.Linq;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.FlashSales.Events;
using FlashSale.Domain.SeedWork;
using FlashSaleEntity = FlashSale.Domain.FlashSales.FlashSale;
using FluentAssertions;
using Xunit;

namespace ecommerce_flashsale_backend.Domain.UnitTests.FlashSales
{
    public class FlashSaleTests
    {
        [Fact]
        public void Activate_ShouldSetStatusToActive_WhenValid()
        {
            // Arrange
            // We use UtcNow.AddMinutes(-5) for start and UtcNow.AddMinutes(5) for end to satisfy spec
            var flashSale = new FlashSaleEntity("Test Sale", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
            flashSale.ClearDomainEvents();

            // Act
            flashSale.Activate();

            // Assert
            flashSale.Status.Should().Be(FlashSaleStatus.Active);
            flashSale.DomainEvents.Should().ContainSingle(e => e is FlashSaleActivatedEvent);
        }

        [Fact]
        public void Activate_ShouldThrowException_WhenNotStartedYet()
        {
            // Arrange
            var flashSale = new FlashSaleEntity("Test Sale", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddMinutes(15));
            
            // Act & Assert
            var action = () => flashSale.Activate();
            action.Should().Throw<DomainException>().WithMessage("Flash sale cannot be activated at this time.");
        }

        [Fact]
        public void End_ShouldSetStatusToEnded()
        {
            // Arrange
            var flashSale = new FlashSaleEntity("Test Sale", DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-5));
            flashSale.ClearDomainEvents();

            // Act
            flashSale.End();

            // Assert
            flashSale.Status.Should().Be(FlashSaleStatus.Ended);
            flashSale.DomainEvents.Should().ContainSingle(e => e is FlashSaleEndedEvent);
        }
    }
}
