namespace FlashSale.Application.CQRS.Payments.Commands.InitiatePayment
{
    public class InitiatePaymentResult
    {
        public bool Success { get; set; }
        public string PaymentUrl { get; set; }
        public string ErrorMessage { get; set; }

        public static InitiatePaymentResult Fail(string message) => new InitiatePaymentResult { Success = false, ErrorMessage = message };
        public static InitiatePaymentResult Ok(string url) => new InitiatePaymentResult { Success = true, PaymentUrl = url };
    }
}
