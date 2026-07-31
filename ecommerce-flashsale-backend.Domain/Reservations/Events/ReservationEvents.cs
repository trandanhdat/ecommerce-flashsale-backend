using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Reservations.Events
{
    public class ReservationCreatedEvent : IDomainEvent
    {
        public Guid ReservationId { get; }
        public Guid FlashSaleItemId { get; }
        public int Quantity { get; }

        public ReservationCreatedEvent(Guid reservationId, Guid flashSaleItemId, int quantity)
        {
            ReservationId = reservationId;
            FlashSaleItemId = flashSaleItemId;
            Quantity = quantity;
        }
    }

    public class ReservationExpiredEvent : IDomainEvent
    {
        public Guid ReservationId { get; }
        public Guid FlashSaleItemId { get; }
        public int Quantity { get; }

        public ReservationExpiredEvent(Guid reservationId, Guid flashSaleItemId, int quantity)
        {
            ReservationId = reservationId;
            FlashSaleItemId = flashSaleItemId;
            Quantity = quantity;
        }
    }

    public class ReservationConvertedEvent : IDomainEvent
    {
        public Guid ReservationId { get; }
        public Guid OrderId { get; }

        public ReservationConvertedEvent(Guid reservationId, Guid orderId)
        {
            ReservationId = reservationId;
            OrderId = orderId;
        }
    }
}
