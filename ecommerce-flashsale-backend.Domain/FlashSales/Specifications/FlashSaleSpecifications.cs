using System;

namespace FlashSale.Domain.FlashSales.Specifications
{
    public class FlashSaleCanBeActivatedSpec
    {
        public bool IsSatisfiedBy(FlashSale flashSale)
        {
            return flashSale.Status == FlashSaleStatus.Upcoming &&
                   flashSale.StartTime <= DateTime.UtcNow &&
                   flashSale.EndTime > DateTime.UtcNow;
        }
    }

    public class FlashSaleItemHasStockSpec
    {
        public bool IsSatisfiedBy(FlashSaleItem item, int quantity)
        {
            return (item.SoldCount + item.ReservedCount + quantity) <= item.SaleStock;
        }
    }
}
