using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmailAsync(string toEmail, Guid orderId, decimal amount, CancellationToken ct);
        Task SendReservationExpiredEmailAsync(string toEmail, Guid reservationId, CancellationToken ct);
    }
}
