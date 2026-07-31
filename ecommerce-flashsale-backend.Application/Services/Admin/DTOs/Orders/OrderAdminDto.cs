using System;
using FlashSale.Domain.Orders;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class OrderAdminDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime PaymentDeadline { get; set; }
        public Guid? ReservationId { get; set; }
    }
}
