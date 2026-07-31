using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Orders.Exceptions
{
    public class OrderCannotBeCancelledException : DomainException
    {
        public OrderCannotBeCancelledException(Guid orderId, OrderStatus currentStatus)
            : base($"Order {orderId} cannot be cancelled because it is in status {currentStatus}.")
        {
        }
    }
}
