using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.ExpireReservations
{
    public class ExpireReservationsCommandHandler : IRequestHandler<ExpireReservationsCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IFlashSaleStockCache _flashSaleStockCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<ExpireReservationsCommandHandler> _logger;

        public ExpireReservationsCommandHandler(
            IReservationRepository reservationRepository,
            IOrderRepository orderRepository,
            IFlashSaleStockCache flashSaleStockCache,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<ExpireReservationsCommandHandler> logger)
        {
            _reservationRepository = reservationRepository;
            _orderRepository = orderRepository;
            _flashSaleStockCache = flashSaleStockCache;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(ExpireReservationsCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // a) Query IReservationRepository lấy tất cả Reservation Status=Holding và ExpiresAt <= UtcNow
            var expiredReservations = await _reservationRepository.GetExpiredHoldingsAsync(now, cancellationToken);
            var reservationList = expiredReservations.ToList();

            if (!reservationList.Any())
            {
                _logger.LogInformation("Đã quét xong. Không có đơn hàng giữ chỗ nào bị quá hạn.");
                return;
            }

            // Lấy toàn bộ Orders liên kết (để tránh N+1 Query)
            var reservationIds = reservationList.Select(r => r.Id).ToList();
            var relatedOrders = await _orderRepository.GetPendingOrdersByReservationIdsAsync(reservationIds, cancellationToken);
            var orderDict = relatedOrders.ToDictionary(o => o.ReservationId!.Value);

            int processedCount = 0;

            // b) Với MỖI Reservation hết hạn
            foreach (var reservation in reservationList)
            {
                // Cập nhật Reservation.Status = Expired
                reservation.Expire();

                // Cập nhật Order liên kết
                if (orderDict.TryGetValue(reservation.Id, out var order))
                {
                    // Order đang Pending -> Cancelled
                    order.Cancel();
                }

                // Gọi IncrementStockAsync để hoàn kho lại đúng Quantity
                await _flashSaleStockCache.IncrementStockAsync(reservation.FlashSaleItemId, reservation.Quantity, cancellationToken);
                
                // Publish MediatR Notification (Đã được tự động hóa qua EF Core Interceptor)
                // Không cần gõ thủ công await _mediator.Publish(...) ở đây nữa.
                
                processedCount++;
            }

            // c) SaveChanges 1 lần cho toàn bộ batch
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // d) Log số lượng Reservation đã xử lý
            _logger.LogInformation("Đã quét và xử lý hoàn kho cho {Count} Reservation(s) hết hạn.", processedCount);
        }
    }
}
