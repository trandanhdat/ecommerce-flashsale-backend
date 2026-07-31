using System;
using System.Collections.Generic;

namespace FlashSale.Application.Common.DTOs
{
    public class ActiveFlashSaleDto
    {
        public Guid FlashSaleId { get; set; }
        public string FlashSaleName { get; set; } = string.Empty;
        public DateTime EndTime { get; set; }
        public List<ActiveFlashSaleItemDto> Items { get; set; } = new();
    }
}
