namespace FlashSale.Application.CQRS.Payments.Commands.ConfirmPaymentCallback
{
    public class ConfirmPaymentCallbackResult
    {
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
    }
}
