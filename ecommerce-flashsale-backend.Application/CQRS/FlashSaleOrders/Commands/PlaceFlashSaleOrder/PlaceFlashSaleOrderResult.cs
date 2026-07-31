using System;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder
{
    public record PlaceFlashSaleOrderResult(
        bool Success,
        Guid? OrderId,
        Guid? ReservationId,
        string? ErrorMessage,
        PlaceFlashSaleOrderErrorCode? ErrorCode
    );
}
