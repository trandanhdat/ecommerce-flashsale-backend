using System.Collections.Generic;
using MediatR;

namespace FlashSale.Application.CQRS.Payments.Commands.ConfirmPaymentCallback
{
    public class ConfirmPaymentCallbackCommand : IRequest<ConfirmPaymentCallbackResult>
    {
        public IDictionary<string, string> CallbackParams { get; set; }
        
        public ConfirmPaymentCallbackCommand(IDictionary<string, string> callbackParams)
        {
            CallbackParams = callbackParams;
        }
    }
}
