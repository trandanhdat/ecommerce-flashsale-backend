using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Application.Common.Interfaces
{
    public interface INotificationHub
    {
        Task NotifyReservationExpiredAsync(Guid userId, Guid reservationId, CancellationToken ct);
        Task NotifyOrderConfirmedAsync(Guid userId, Guid orderId, CancellationToken ct);
        Task NotifySaleOpeningAsync(Guid flashSaleId, string flashSaleName, CancellationToken ct);
    }
}
