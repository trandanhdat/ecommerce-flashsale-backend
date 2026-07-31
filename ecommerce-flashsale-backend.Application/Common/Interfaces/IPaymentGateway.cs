using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentUrlResult> CreatePaymentUrlAsync(PaymentRequestDto request, CancellationToken ct);
        Task<bool> VerifyCallbackSignatureAsync(IDictionary<string, string> callbackParams, CancellationToken ct);
    }
}
