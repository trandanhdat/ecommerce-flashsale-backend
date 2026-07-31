using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlashSale.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private SmtpClient CreateSmtpClient()
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            return new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };
        }

        public async Task SendOrderConfirmationEmailAsync(string toEmail, Guid orderId, decimal amount, CancellationToken ct)
        {
            try
            {
                using var client = CreateSmtpClient();
                var fromEmail = _configuration["Smtp:FromEmail"];
                var fromName = _configuration["Smtp:FromName"];

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = $"[FlashSale] Xác nhận đơn hàng {orderId}",
                    Body = $@"
                        <h3>Cảm ơn bạn đã mua sắm tại FlashSale!</h3>
                        <p>Đơn hàng của bạn đã được thanh toán thành công.</p>
                        <ul>
                            <li><strong>Mã đơn hàng:</strong> {orderId}</li>
                            <li><strong>Tổng tiền:</strong> {amount:N0} VNĐ</li>
                        </ul>
                        <p>Chúng tôi sẽ sớm giao hàng cho bạn.</p>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage, ct);
                _logger.LogInformation("Đã gửi Email xác nhận đơn hàng {OrderId} tới {Email}", orderId, toEmail);
            }
            catch (Exception ex)
            {
                // FAIL-SAFE: Chỉ log lỗi, không ném Exception làm hỏng tiến trình
                _logger.LogError(ex, "Lỗi khi gửi Email xác nhận cho đơn hàng {OrderId} tới {Email}", orderId, toEmail);
            }
        }

        public async Task SendReservationExpiredEmailAsync(string toEmail, Guid reservationId, CancellationToken ct)
        {
            try
            {
                using var client = CreateSmtpClient();
                var fromEmail = _configuration["Smtp:FromEmail"];
                var fromName = _configuration["Smtp:FromName"];

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = $"[FlashSale] Thông báo huỷ giữ chỗ {reservationId}",
                    Body = $@"
                        <h3>Thông báo huỷ giữ chỗ</h3>
                        <p>Phiên giữ chỗ của bạn ({reservationId}) đã hết hạn do không thanh toán đúng hạn.</p>
                        <p>Hệ thống đã tự động hoàn lại số lượng sản phẩm vào kho Flash Sale.</p>
                        <p>Hẹn gặp lại bạn ở đợt mở bán tiếp theo!</p>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage, ct);
                _logger.LogInformation("Đã gửi Email báo huỷ giữ chỗ {ReservationId} tới {Email}", reservationId, toEmail);
            }
            catch (Exception ex)
            {
                // FAIL-SAFE: Chỉ log lỗi, không ném Exception làm hỏng tiến trình
                _logger.LogError(ex, "Lỗi khi gửi Email báo huỷ giữ chỗ {ReservationId} tới {Email}", reservationId, toEmail);
            }
        }
    }
}
