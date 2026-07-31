using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Reservations.Events;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.EventHandlers
{
    public class ReservationExpiredEventHandler : INotificationHandler<ReservationExpiredEvent>
    {
        private readonly IEmailService _emailService;
        private readonly INotificationHub _notificationHub;
        private readonly IUserRepository _userRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<ReservationExpiredEventHandler> _logger;

        public ReservationExpiredEventHandler(
            IEmailService emailService,
            INotificationHub notificationHub,
            IUserRepository userRepository,
            IReservationRepository reservationRepository,
            ILogger<ReservationExpiredEventHandler> logger)
        {
            _emailService = emailService;
            _notificationHub = notificationHub;
            _userRepository = userRepository;
            _reservationRepository = reservationRepository;
            _logger = logger;
        }

        public async Task Handle(ReservationExpiredEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ReservationExpiredEventHandler: Xử lý Notification gửi Email & SignalR cho ReservationId: {ReservationId}", notification.ReservationId);

            var reservation = await _reservationRepository.GetByIdAsync(notification.ReservationId);
            if (reservation == null)
            {
                _logger.LogWarning("Không tìm thấy Reservation để gửi thông báo huỷ giữ chỗ.");
                return;
            }

            var user = await _userRepository.GetByIdAsync(reservation.UserId);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy User để gửi thông báo huỷ giữ chỗ.");
                return;
            }

            // 1. Gửi Email (Bắt lỗi bên trong IEmailService để không crash tiến trình)
            await _emailService.SendReservationExpiredEmailAsync(user.Email, notification.ReservationId, cancellationToken);

            // 2. Bắn SignalR Realtime (Server Push)
            await _notificationHub.NotifyReservationExpiredAsync(user.Id, notification.ReservationId, cancellationToken);
        }
    }
}
