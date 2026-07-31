using System;
using MediatR;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.UpdateFlashSaleStock
{
    public record UpdateFlashSaleStockCommand(Guid FlashSaleItemId, int QuantityToAdd) : IRequest<bool>;
}
