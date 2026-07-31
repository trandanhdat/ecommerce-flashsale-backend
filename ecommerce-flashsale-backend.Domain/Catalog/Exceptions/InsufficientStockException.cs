using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Catalog.Exceptions
{
    public class InsufficientStockException : DomainException
    {
        public InsufficientStockException(Guid productId, int requestedQuantity)
            : base($"Insufficient stock for product {productId}. Requested: {requestedQuantity}")
        {
        }
    }
}
