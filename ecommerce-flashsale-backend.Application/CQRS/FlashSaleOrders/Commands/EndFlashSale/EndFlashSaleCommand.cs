using MediatR;
using System;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.EndFlashSale
{
    public record EndFlashSaleCommand(Guid FlashSaleId) : IRequest;
}
