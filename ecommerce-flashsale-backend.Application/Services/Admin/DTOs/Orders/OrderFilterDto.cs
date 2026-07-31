using FlashSale.Domain.Orders;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class OrderFilterDto
    {
        public OrderStatus? Status { get; set; }
        public OrderType? Type { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
