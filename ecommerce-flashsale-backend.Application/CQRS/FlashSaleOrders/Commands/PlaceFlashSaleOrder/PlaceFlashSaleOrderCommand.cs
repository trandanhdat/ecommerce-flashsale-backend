using System;
using MediatR;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder
{
    public record PlaceFlashSaleOrderCommand(Guid FlashSaleItemId, int Quantity) : IRequest<PlaceFlashSaleOrderResult>;
}
