using System;

namespace FlashSale.Application.Services.Cart.DTOs
{
    public class AddCartItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
