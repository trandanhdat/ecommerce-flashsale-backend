using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Application.Common.Events
{
    public class OrderConfirmedIntegrationEvent
    {
        public Guid EventId { get; }
        public DateTime OccurredOn { get; }
        public Guid OrderId { get; }
        public Guid UserId { get; }

        public OrderConfirmedIntegrationEvent(Guid orderId, Guid userId)
        {
            EventId = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
            OrderId = orderId;
            UserId = userId;
        }
    }
}
