using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Orders
{
    public class OrderItem : Entity
    {
        public Guid OrderId { get; private set; }
        public Guid ProductId { get; private set; }
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        protected OrderItem() { }

        internal OrderItem(Guid orderId, Guid productId, decimal price, int quantity)
        {
            Id = Guid.NewGuid();
            OrderId = orderId;
            ProductId = productId;
            Price = price;
            Quantity = quantity;
        }
    }
}
