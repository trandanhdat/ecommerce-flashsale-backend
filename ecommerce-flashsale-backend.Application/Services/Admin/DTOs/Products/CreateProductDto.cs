using System;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class CreateProductDto
    {
        public Guid CategoryId { get; set; }
        public string? SKU { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal BasePrice { get; set; }
        public int StockQuantity { get; set; }
    }
}
