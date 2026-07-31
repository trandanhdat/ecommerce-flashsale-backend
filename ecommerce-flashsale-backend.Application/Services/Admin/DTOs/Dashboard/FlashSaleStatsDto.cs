using System;
using System.Collections.Generic;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class FlashSaleStatsDto
    {
        // TODO: Chưa có chức năng Tracking Views ở entity FlashSale. Tạm thời trả về 0.
        public int TotalViews { get; set; }
        
        public int TotalParticipants { get; set; }
        
        // Tỉ lệ chuyển đổi = Số đơn hàng / Số lượt giữ chỗ (Reservation)
        public double ConversionRate { get; set; }

        public List<FlashSaleItemStatDto> Items { get; set; } = new List<FlashSaleItemStatDto>();
    }

    public class FlashSaleItemStatDto
    {
        public Guid FlashSaleItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        public int RemainingStock { get; set; }
    }
}
