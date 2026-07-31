using MediatR;
using System;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.ActivateFlashSale
{
    public record ActivateFlashSaleCommand(Guid FlashSaleId) : IRequest;
}
