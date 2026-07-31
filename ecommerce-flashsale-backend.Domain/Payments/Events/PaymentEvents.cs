using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Payments.Events
{
    public class PaymentSucceededEvent : IDomainEvent
    {
        public Guid PaymentId { get; }
        public Guid OrderId { get; }

        public PaymentSucceededEvent(Guid paymentId, Guid orderId)
        {
            PaymentId = paymentId;
            OrderId = orderId;
        }
    }

    public class PaymentFailedEvent : IDomainEvent
    {
        public Guid PaymentId { get; }
        public Guid OrderId { get; }

        public PaymentFailedEvent(Guid paymentId, Guid orderId)
        {
            PaymentId = paymentId;
            OrderId = orderId;
        }
    }
}
