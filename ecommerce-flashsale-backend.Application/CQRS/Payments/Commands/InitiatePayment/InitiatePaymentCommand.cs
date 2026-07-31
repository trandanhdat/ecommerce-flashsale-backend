using System;
using MediatR;

namespace FlashSale.Application.CQRS.Payments.Commands.InitiatePayment
{
    public record InitiatePaymentCommand(Guid OrderId, string ClientIpAddress) : IRequest<InitiatePaymentResult>;
}
