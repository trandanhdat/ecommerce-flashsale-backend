using System;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Reservations.Exceptions;
using FlashSale.Domain.Reservations.Events;

namespace FlashSale.Domain.Reservations
{
    public class Reservation : AggregateRoot
    {
        public Guid FlashSaleItemId { get; private set; }
        public Guid UserId { get; private set; }
        public int Quantity { get; private set; }
        public ReservationStatus Status { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Guid? OrderId { get; private set; }
        public byte[] RowVersion { get; private set; }

        protected Reservation() { }

        public Reservation(Guid flashSaleItemId, Guid userId, int quantity, DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            FlashSaleItemId = flashSaleItemId;
            UserId = userId;
            Quantity = quantity;
            Status = ReservationStatus.Holding;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;

            AddDomainEvent(new ReservationCreatedEvent(Id, FlashSaleItemId, Quantity));
        }

        public void Expire()
        {
            if (Status != ReservationStatus.Holding) return;

            Status = ReservationStatus.Expired;
            AddDomainEvent(new ReservationExpiredEvent(Id, FlashSaleItemId, Quantity));
        }

        public void ConvertToOrder(Guid orderId)
        {
            if (Status == ReservationStatus.Expired)
            {
                throw new ReservationAlreadyExpiredException(Id);
            }

            Status = ReservationStatus.Converted;
            OrderId = orderId;
            AddDomainEvent(new ReservationConvertedEvent(Id, OrderId.Value));
        }
    }
}
