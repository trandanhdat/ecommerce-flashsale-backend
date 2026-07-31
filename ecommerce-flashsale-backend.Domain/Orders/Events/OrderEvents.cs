using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Orders.Events
{
    public class OrderCreatedEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public OrderCreatedEvent(Guid orderId) => OrderId = orderId;
    }

    public class OrderConfirmedEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public OrderConfirmedEvent(Guid orderId) => OrderId = orderId;
    }

    public class OrderCancelledEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public OrderCancelledEvent(Guid orderId) => OrderId = orderId;
    }
}
