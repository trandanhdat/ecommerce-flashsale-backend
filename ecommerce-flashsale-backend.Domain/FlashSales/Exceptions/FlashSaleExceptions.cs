using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.FlashSales.Exceptions
{
    public class FlashSaleNotActiveException : DomainException
    {
        public FlashSaleNotActiveException(Guid flashSaleId)
            : base($"Flash sale {flashSaleId} is not active.")
        {
        }
    }

    public class FlashSaleStockExceededException : DomainException
    {
        public FlashSaleStockExceededException(Guid flashSaleItemId)
            : base($"Not enough stock available for flash sale item {flashSaleItemId}.")
        {
        }
    }
}
