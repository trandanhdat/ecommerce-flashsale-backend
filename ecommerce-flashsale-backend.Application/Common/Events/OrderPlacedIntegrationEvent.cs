using System;

namespace FlashSale.Application.Common.Events
{
    public class OrderPlacedIntegrationEvent
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public Guid FlashSaleItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
