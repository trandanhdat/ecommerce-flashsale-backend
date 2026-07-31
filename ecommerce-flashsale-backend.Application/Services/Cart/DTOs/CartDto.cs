using System.Collections.Generic;

namespace FlashSale.Application.Services.Cart.DTOs
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalAmount { get; set; }
    }
}
