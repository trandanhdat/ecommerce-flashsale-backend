using System;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class RevenueByDateDto
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
