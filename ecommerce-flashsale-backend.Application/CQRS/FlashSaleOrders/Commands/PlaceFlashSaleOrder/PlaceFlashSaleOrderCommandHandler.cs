using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Enums;
using FlashSale.Application.Common.Events;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder
{
    public class PlaceFlashSaleOrderCommandHandler : IRequestHandler<PlaceFlashSaleOrderCommand, PlaceFlashSaleOrderResult>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IFlashSaleStockCache _flashSaleStockCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDistributedLockService _lockService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlaceFlashSaleOrderCommandHandler> _logger;

        public PlaceFlashSaleOrderCommandHandler(
            IFlashSaleRepository flashSaleRepository,
            IReservationRepository reservationRepository,
            IOrderRepository orderRepository,
            IFlashSaleStockCache flashSaleStockCache,
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            ICurrentUserService currentUserService,
            IDistributedLockService lockService,
            IConfiguration configuration,
            ILogger<PlaceFlashSaleOrderCommandHandler> logger)
        {
            _flashSaleRepository = flashSaleRepository;
            _reservationRepository = reservationRepository;
            _orderRepository = orderRepository;
            _flashSaleStockCache = flashSaleStockCache;
            _unitOfWork = unitOfWork;
            _eventPublisher = eventPublisher;
            _currentUserService = currentUserService;
            _lockService = lockService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PlaceFlashSaleOrderResult> Handle(PlaceFlashSaleOrderCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null || userId == Guid.Empty)
            {
                return new PlaceFlashSaleOrderResult(false, null, null, "User chưa đăng nhập.", null);
            }

            var actualUserId = userId.Value;
            var lockKey = $"lock:place_order:{actualUserId}:{request.FlashSaleItemId}";

            // Acquire lock to prevent the same user from spamming requests concurrently
            bool lockAcquired = await _lockService.TryAcquireLockAsync(lockKey, TimeSpan.FromSeconds(5), cancellationToken);
            if (!lockAcquired)
            {
                _logger.LogWarning("PlaceFlashSaleOrder rate-limited for User {UserId}: (Item {ItemId})", actualUserId, request.FlashSaleItemId);
                return new PlaceFlashSaleOrderResult(false, null, null, "Yêu cầu đang được xử lý, vui lòng không bấm liên tục.", null);
            }

            try
            {
                // a) Kiểm tra FlashSaleItem tồn tại + FlashSale đang Status=Active
                var flashSaleItem = await _flashSaleRepository.GetActiveItemByIdAsync(request.FlashSaleItemId, cancellationToken);
                if (flashSaleItem == null)
                {
                    _logger.LogWarning("PlaceFlashSaleOrder failed for User {UserId}: SaleNotActive (Item {ItemId})", actualUserId, request.FlashSaleItemId);
                    return new PlaceFlashSaleOrderResult(false, null, null, "Flash Sale không tồn tại hoặc chưa bắt đầu/đã kết thúc.", PlaceFlashSaleOrderErrorCode.SaleNotActive);
                }

                // b) Kiểm tra user này đã có Reservation Status=Holding cho CHÍNH FlashSaleItem này chưa
                var existingReservation = await _reservationRepository.GetHoldingByUserAndItemAsync(actualUserId, request.FlashSaleItemId, cancellationToken);
                if (existingReservation != null)
                {
                    _logger.LogWarning("PlaceFlashSaleOrder failed for User {UserId}: AlreadyHasPendingReservation (Item {ItemId})", actualUserId, request.FlashSaleItemId);
                    return new PlaceFlashSaleOrderResult(false, null, null, "Bạn đã có đơn hàng giữ chỗ đang chờ thanh toán cho sản phẩm này.", PlaceFlashSaleOrderErrorCode.AlreadyHasPendingReservation);
                }

                // c) Gọi IFlashSaleStockCache.TryDecrementStockAsync (Atomic Decrement)
                var decrementResult = await _flashSaleStockCache.TryDecrementStockAsync(request.FlashSaleItemId, request.Quantity, cancellationToken);
                
                if (decrementResult == StockDecrementResult.StockNotInitialized)
                {
                    _logger.LogError("LỖI HỆ THỐNG: Cố gắng mua FlashSaleItem {ItemId} nhưng Redis chưa được Init tồn kho!", request.FlashSaleItemId);
                    return new PlaceFlashSaleOrderResult(false, null, null, "Hệ thống chưa sẵn sàng, vui lòng thử lại sau.", PlaceFlashSaleOrderErrorCode.StockNotInitialized);
                }

                if (decrementResult == StockDecrementResult.InsufficientStock)
                {
                    return new PlaceFlashSaleOrderResult(false, null, null, "Sản phẩm đã hết hàng.", PlaceFlashSaleOrderErrorCode.InsufficientStock);
                }

                // d) DECR Redis thành công → Bắt đầu Block Transaction (Tạo Reservation & Order)
                var holdingMinutes = int.Parse(_configuration["FlashSale:ReservationHoldingMinutes"] ?? "5");
                var expiresAt = DateTime.UtcNow.AddMinutes(holdingMinutes);

                // e) Tạo Order mới qua Order.Create(...) / factory method
                var reservation = new Reservation(request.FlashSaleItemId, actualUserId, request.Quantity, expiresAt);
                
                var paymentDeadline = expiresAt; // Cùng hạn với Reservation
                var order = new Order(actualUserId, OrderType.FlashSale, "N/A", "N/A", "N/A", paymentDeadline, reservation.Id);
                order.AddOrderItem(flashSaleItem.ProductId, flashSaleItem.SalePrice.Amount, request.Quantity);

                try
                {
                    // Thêm vào DbContext nhưng CHƯA save (void Add)
                    _reservationRepository.Add(reservation);
                    _orderRepository.Add(order);

                    // f) SaveChanges (Reservation + Order trong CÙNG 1 transaction DbContext)
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // g) NẾU bước f (ghi DB) thất bại SAU KHI đã DECR Redis thành công ở bước c 
                    // → PHẢI rollback Redis bằng cách gọi IncrementStockAsync để hoàn kho lại (compensating action)
                    _logger.LogError(ex, "LỖI KHI LƯU DATABASE TẠI PlaceFlashSaleOrderCommand! Bắt đầu Rollback Redis cho Item: {ItemId}, Quantity: {Quantity}", request.FlashSaleItemId, request.Quantity);
                    
                    await _flashSaleStockCache.IncrementStockAsync(request.FlashSaleItemId, request.Quantity, cancellationToken);
                    
                    return new PlaceFlashSaleOrderResult(false, null, null, "Lỗi hệ thống khi lưu đơn hàng, đã hoàn lại kho thành công.", null);
                }

                // h) Publish Integration Event (Mock NoOp)
                var integrationEvent = new OrderPlacedIntegrationEvent
                {
                    OrderId = order.Id,
                    UserId = actualUserId,
                    FlashSaleItemId = request.FlashSaleItemId,
                    Quantity = request.Quantity,
                    ExpiresAt = expiresAt
                };
                
                await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

                // i) Trả PlaceFlashSaleOrderResult thành công
                _logger.LogInformation("PlaceFlashSaleOrder success for User {UserId}: Order {OrderId} (Item {ItemId}, Quantity {Quantity})", actualUserId, order.Id, request.FlashSaleItemId, request.Quantity);
                return new PlaceFlashSaleOrderResult(true, order.Id, reservation.Id, null, null);
            }
            finally
            {
                // Always release the lock so the user can try again later if they need to.
                await _lockService.ReleaseLockAsync(lockKey, cancellationToken);
            }
        }
    }
}
