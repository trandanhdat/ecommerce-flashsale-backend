using System;

namespace FlashSale.Application.Common.DTOs
{
    public class ProductCatalogCacheDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int StockQuantity { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool HasActiveFlashSale { get; set; }
        public decimal? FlashSalePrice { get; set; }
    }
}
