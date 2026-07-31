namespace FlashSale.Domain.Orders
{
    public enum OrderType
    {
        Normal,
        FlashSale
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}
