namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder
{
    public enum PlaceFlashSaleOrderErrorCode
    {
        SaleNotActive,
        InsufficientStock,
        StockNotInitialized,
        ExceedMaxQuantityPerUser,
        AlreadyHasPendingReservation
    }
}
