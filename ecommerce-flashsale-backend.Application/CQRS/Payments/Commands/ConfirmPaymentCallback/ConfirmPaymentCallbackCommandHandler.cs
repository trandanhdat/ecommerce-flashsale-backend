using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Events;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Payments;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.Payments.Commands.ConfirmPaymentCallback
{
    public class ConfirmPaymentCallbackCommandHandler : IRequestHandler<ConfirmPaymentCallbackCommand, ConfirmPaymentCallbackResult>
    {
        private readonly IPaymentGateway _paymentGateway;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventPublisher _eventPublisher;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly ILogger<ConfirmPaymentCallbackCommandHandler> _logger;

        public ConfirmPaymentCallbackCommandHandler(
            IPaymentGateway paymentGateway,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IReservationRepository reservationRepository,
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            IConfiguration configuration,
            IMediator mediator,
            ILogger<ConfirmPaymentCallbackCommandHandler> logger)
        {
            _paymentGateway = paymentGateway;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _reservationRepository = reservationRepository;
            _unitOfWork = unitOfWork;
            _eventPublisher = eventPublisher;
            _configuration = configuration;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ConfirmPaymentCallbackResult> Handle(ConfirmPaymentCallbackCommand request, CancellationToken cancellationToken)
        {
            var frontendUrl = _configuration["Frontend:PaymentResultUrl"] ?? "http://localhost:3000/payment-result";

            // 1. Verify Signature TRƯỚC TIÊN (Chống giả mạo)
            var isValid = await _paymentGateway.VerifyCallbackSignatureAsync(request.CallbackParams, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("Invalid callback signature.");
                return new ConfirmPaymentCallbackResult 
                { 
                    Success = false, 
                    RedirectUrl = $"{frontendUrl}?success=false&reason=invalid_signature" 
                };
            }

            // Lấy các tham số cơ bản
            request.CallbackParams.TryGetValue("vnp_TxnRef", out var transactionCode);
            request.CallbackParams.TryGetValue("vnp_ResponseCode", out var responseCode);
            request.CallbackParams.TryGetValue("vnp_TransactionNo", out var vnpTransactionNo);

            if (string.IsNullOrEmpty(transactionCode))
            {
                _logger.LogError("Missing vnp_TxnRef in callback.");
                return new ConfirmPaymentCallbackResult { Success = false, RedirectUrl = $"{frontendUrl}?success=false&reason=missing_txnref" };
            }

            // 2. Load Payment
            var payment = await _paymentRepository.GetByTransactionNoAsync(transactionCode, cancellationToken);
            if (payment == null)
            {
                _logger.LogError("Payment with TransactionCode {TransactionCode} not found.", transactionCode);
                return new ConfirmPaymentCallbackResult { Success = false, RedirectUrl = $"{frontendUrl}?success=false&reason=payment_not_found" };
            }

            // 3. IDEMPOTENCY CHECK (QUAN TRỌNG NHẤT)
            // Nếu Payment ĐÃ LÀ Success hoặc Failed, ta BỎ QUA không xử lý lại, trả về luôn RedirectUrl.
            // Giải thích: VNPay có thể gọi webhook nhiều lần cho cùng 1 giao dịch, hoặc User F5 trình duyệt.
            // Nếu xử lý lại sẽ làm cộng tiền 2 lần hoặc văng lỗi do Order đã Confirmed.
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation("Idempotency hit: Payment {PaymentId} is already {Status}. Skipping processing.", payment.Id, payment.Status);
                var isAlreadySuccess = payment.Status == PaymentStatus.Success;
                return new ConfirmPaymentCallbackResult 
                { 
                    Success = isAlreadySuccess, 
                    RedirectUrl = $"{frontendUrl}?success={isAlreadySuccess.ToString().ToLower()}&orderId={payment.OrderId}" 
                };
            }

            // 4. Kiểm tra mã lỗi từ VNPay ("00" là thành công, các mã khác là lỗi/huỷ)
            var isSuccess = responseCode == "00";

            if (isSuccess)
            {
                _logger.LogInformation("ConfirmPaymentCallback success for Payment {PaymentId}: Transaction {TransactionCode} (ResponseCode 00)", payment.Id, transactionCode);
                // Thanh toán thành công
                payment.MarkAsSuccess(vnpTransactionNo); // Lưu lại vnp_TransactionNo của hệ thống VNPay

                // Load Order
                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    // Chuyển sang trạng thái Confirmed (Đã chốt)
                    order.Confirm();
                    
                    // Lấy Reservation và Convert (để khỏi bị Hangfire hiểu nhầm là chưa thanh toán)
                    if (order.ReservationId.HasValue)
                    {
                        var reservation = await _reservationRepository.GetByIdAsync(order.ReservationId.Value);
                        if (reservation != null && reservation.Status == FlashSale.Domain.Reservations.ReservationStatus.Holding)
                        {
                            reservation.ConvertToOrder(order.Id);
                        }
                    }

                    // 1. Publish Integration Event
                    // Dành cho các Microservices khác lắng nghe. (Hiện tại đang dùng Mock)
                    await _eventPublisher.PublishAsync(new FlashSale.Application.Common.Events.OrderConfirmedIntegrationEvent(order.Id, order.UserId), cancellationToken);

                    // 2. Publish MediatR Notification (Đã được tự động hóa qua EF Core Interceptor)
                    // Không cần gõ lại thủ công ở đây nữa!
                }
            }
            else
            {
                // Thanh toán thất bại hoặc user bấm Huỷ
                payment.MarkAsFailed(vnpTransactionNo);
                
                // Chú ý: Ta KHÔNG huỷ (Cancel) Order ngay lập tức. 
                // User vẫn có thể thử thanh toán lại bằng thẻ khác trước khi PaymentDeadline hết hạn.
                // Job ExpireReservations sẽ tự động huỷ Order nếu quá hạn thật sự.
                _logger.LogInformation("Payment {PaymentId} failed with response code {ResponseCode}.", payment.Id, responseCode);
            }

            // 5. Lưu vào Database (cả Payment và Order sẽ được lưu trong cùng 1 transaction EF Core)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Xây dựng Redirect URL cho Front-end
            return new ConfirmPaymentCallbackResult 
            { 
                Success = isSuccess, 
                RedirectUrl = $"{frontendUrl}?success={isSuccess.ToString().ToLower()}&orderId={payment.OrderId}" 
            };
        }
    }
}
