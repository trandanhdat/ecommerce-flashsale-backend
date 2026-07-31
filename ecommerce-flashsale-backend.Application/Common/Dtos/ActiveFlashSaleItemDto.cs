using System;

namespace FlashSale.Application.Common.DTOs
{
    public class ActiveFlashSaleItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        
        // Tính tồn kho còn lại của chương trình Flash Sale
        public int RemainingStock => SaleStock - SoldCount;
    }
}
