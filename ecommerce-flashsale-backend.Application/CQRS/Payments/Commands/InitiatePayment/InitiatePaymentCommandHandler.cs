using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Payments;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.Payments.Commands.InitiatePayment
{
    public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResult>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InitiatePaymentCommandHandler> _logger;

        public InitiatePaymentCommandHandler(
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IPaymentGateway paymentGateway,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<InitiatePaymentCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _paymentGateway = paymentGateway;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<InitiatePaymentResult> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return InitiatePaymentResult.Fail("Unauthorized.");
            }

            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                return InitiatePaymentResult.Fail("Order not found.");
            }

            if (order.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to pay for Order {OrderId} belonging to another user.", userId, request.OrderId);
                return InitiatePaymentResult.Fail("Order not found."); // Chống IDOR
            }

            if (order.Status != OrderStatus.Pending)
            {
                return InitiatePaymentResult.Fail($"Order is not in Pending status. Current status: {order.Status}");
            }

            if (order.PaymentDeadline < DateTime.UtcNow)
            {
                return InitiatePaymentResult.Fail("The payment deadline for this order has expired. Please place a new order.");
            }

            // Xoá payment cũ nếu có để tránh trùng
            var existingPayment = await _paymentRepository.GetPendingPaymentByOrderIdAsync(order.Id, cancellationToken);
            if (existingPayment != null)
            {
                _logger.LogInformation("Found existing pending payment {PaymentId} for Order {OrderId}. Deleting it.", existingPayment.Id, order.Id);
                _paymentRepository.Delete(existingPayment);
            }

            // Tạo Payment mới
            var payment = new Payment(order.Id, PaymentProvider.VNPay, order.TotalAmount);
            await _paymentRepository.AddAsync(payment);

            // Sinh TransactionCode = Id của Payment (vừa là unique, vừa dùng để đối soát)
            var transactionCode = payment.Id.ToString("N");

            // Build request gửi sang Gateway
            var pr = new PaymentRequestDto
            {
                OrderId = order.Id,
                TransactionCode = transactionCode,
                Amount = order.TotalAmount,
                ClientIpAddress = request.ClientIpAddress
            };

            var urlResult = await _paymentGateway.CreatePaymentUrlAsync(pr, cancellationToken);
            if (string.IsNullOrEmpty(urlResult.PaymentUrl))
            {
                return InitiatePaymentResult.Fail("Failed to generate payment URL from gateway.");
            }

            // Lưu DB trước khi trả URL cho user (đảm bảo transaction code tồn tại)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully initiated payment for Order {OrderId} with TransactionCode {TransactionCode}.", order.Id, transactionCode);
            
            return InitiatePaymentResult.Ok(urlResult.PaymentUrl);
        }
    }
}
