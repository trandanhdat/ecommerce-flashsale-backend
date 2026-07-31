using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FlashSale.Infrastructure.Realtime
{
    public class SignalRNotificationHub : INotificationHub
    {
        private readonly IHubContext<FlashSaleHub> _hubContext;

        public SignalRNotificationHub(IHubContext<FlashSaleHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOrderConfirmedAsync(Guid userId, Guid orderId, CancellationToken ct)
        {
            await _hubContext.Clients.Group($"user:{userId}").SendAsync("OrderConfirmed", new { OrderId = orderId }, ct);
        }

        public async Task NotifyReservationExpiredAsync(Guid userId, Guid reservationId, CancellationToken ct)
        {
            await _hubContext.Clients.Group($"user:{userId}").SendAsync("ReservationExpired", new { ReservationId = reservationId }, ct);
        }

        public async Task NotifySaleOpeningAsync(Guid flashSaleId, string flashSaleName, CancellationToken ct)
        {
            // Broadcast cho toàn bộ user đang kết nối
            await _hubContext.Clients.All.SendAsync("SaleOpening", new { FlashSaleId = flashSaleId, FlashSaleName = flashSaleName }, ct);
        }
    }
}
