using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Orders.Events;
using FlashSale.Domain.Users;
using FlashSale.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.EventHandlers
{
    public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly INotificationHub _notificationHub;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderConfirmedEventHandler> _logger;

        public OrderConfirmedEventHandler(
            IEmailService emailService,
            INotificationHub notificationHub,
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            ILogger<OrderConfirmedEventHandler> logger)
        {
            _emailService = emailService;
            _notificationHub = notificationHub;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("OrderConfirmedEventHandler: Xử lý Notification gửi Email & SignalR cho OrderId: {OrderId}", notification.OrderId);

            var order = await _orderRepository.GetByIdAsync(notification.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Không tìm thấy Order để gửi thông báo xác nhận.");
                return;
            }

            var user = await _userRepository.GetByIdAsync(order.UserId);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy User để gửi thông báo xác nhận.");
                return;
            }

            // 1. Gửi Email (Bắt lỗi bên trong IEmailService để không crash tiến trình)
            await _emailService.SendOrderConfirmationEmailAsync(user.Email, order.Id, order.TotalAmount, cancellationToken);

            // 2. Bắn SignalR Realtime (Server Push)
            await _notificationHub.NotifyOrderConfirmedAsync(order.UserId, order.Id, cancellationToken);
        }
    }
}
