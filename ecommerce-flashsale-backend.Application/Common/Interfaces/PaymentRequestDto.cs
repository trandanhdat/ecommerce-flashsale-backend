using System;

namespace FlashSale.Application.Common.Interfaces
{
    public class PaymentRequestDto
    {
        public Guid OrderId { get; set; }
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public string ClientIpAddress { get; set; }
    }
}
