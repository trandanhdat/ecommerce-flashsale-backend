using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users
{
    public class CartItem : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        protected CartItem() { }

        public static CartItem Create(Guid userId, Guid productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.", nameof(quantity));

            return new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.", nameof(newQuantity));

            Quantity = newQuantity;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddQuantity(int quantityToAdd)
        {
            if (quantityToAdd <= 0)
                throw new ArgumentException("Quantity to add must be greater than 0.", nameof(quantityToAdd));

            Quantity += quantityToAdd;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
