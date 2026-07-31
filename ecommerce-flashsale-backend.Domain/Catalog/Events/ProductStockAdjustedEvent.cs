using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Catalog.Events
{
    public class ProductStockAdjustedEvent : IDomainEvent
    {
        public Guid ProductId { get; }
        public int NewStock { get; }

        public ProductStockAdjustedEvent(Guid productId, int newStock)
        {
            ProductId = productId;
            NewStock = newStock;
        }
    }
}
