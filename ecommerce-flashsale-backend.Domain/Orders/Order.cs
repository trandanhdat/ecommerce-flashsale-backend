using System;
using System.Collections.Generic;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Orders.Exceptions;
using FlashSale.Domain.Orders.Events;

namespace FlashSale.Domain.Orders
{
    public class Order : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public OrderType Type { get; private set; }
        public Guid? ReservationId { get; private set; }
        public decimal TotalAmount { get; private set; }
        public OrderStatus Status { get; private set; }

        public string ReceiverName { get; private set; }
        public string ReceiverPhone { get; private set; }
        public string ShippingAddress { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime PaymentDeadline { get; private set; }
        public byte[] RowVersion { get; private set; }

        private readonly List<OrderItem> _orderItems = new List<OrderItem>();
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        protected Order() { }

        public Order(Guid userId, OrderType type, string receiverName, string receiverPhone, string shippingAddress, DateTime paymentDeadline, Guid? reservationId = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Type = type;
            ReceiverName = receiverName;
            ReceiverPhone = receiverPhone;
            ShippingAddress = shippingAddress;
            PaymentDeadline = paymentDeadline;
            ReservationId = reservationId;
            Status = OrderStatus.Pending;
            CreatedAt = DateTime.UtcNow;

            AddDomainEvent(new OrderCreatedEvent(Id));
        }

        public void AddOrderItem(Guid productId, decimal price, int quantity)
        {
            var item = new OrderItem(Id, productId, price, quantity);
            _orderItems.Add(item);
            CalculateTotalAmount();
        }

        private void CalculateTotalAmount()
        {
            TotalAmount = 0;
            foreach (var item in _orderItems)
            {
                TotalAmount += item.Price * item.Quantity;
            }
        }

        public void Confirm()
        {
            if (Status == OrderStatus.Pending)
            {
                Status = OrderStatus.Confirmed;
                AddDomainEvent(new OrderConfirmedEvent(Id));
            }
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Completed || Status == OrderStatus.Confirmed)
            {
                throw new OrderCannotBeCancelledException(Id, Status);
            }

            Status = OrderStatus.Cancelled;
            AddDomainEvent(new OrderCancelledEvent(Id));
        }
    }
}
