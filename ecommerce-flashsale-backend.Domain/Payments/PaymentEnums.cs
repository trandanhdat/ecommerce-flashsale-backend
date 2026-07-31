namespace FlashSale.Domain.Payments
{
    public enum PaymentProvider
    {
        VNPay,
        MoMo,
        COD
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed
    }
}
