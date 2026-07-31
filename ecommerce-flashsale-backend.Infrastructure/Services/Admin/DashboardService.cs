using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Reservations;
using FlashSale.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using FlashSale.Application.Services.Admin;
using FlashSale.Infrastructure.Persistence;

namespace FlashSale.Infrastructure.Services.Admin
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOrderRepository _orderRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFlashSaleStockCache _flashSaleStockCache;

        public DashboardService(
            ApplicationDbContext dbContext,
            IOrderRepository orderRepository,
            IReservationRepository reservationRepository,
            IUnitOfWork unitOfWork,
            IFlashSaleStockCache flashSaleStockCache)
        {
            _dbContext = dbContext;
            _orderRepository = orderRepository;
            _reservationRepository = reservationRepository;
            _unitOfWork = unitOfWork;
            _flashSaleStockCache = flashSaleStockCache;
        }

        public async Task<FlashSaleStatsDto> GetFlashSaleStatsAsync(Guid flashSaleId, CancellationToken ct = default)
        {
            var flashSale = await _dbContext.FlashSales
                .AsNoTracking()
                .Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.Id == flashSaleId, ct);

            if (flashSale == null)
                throw new Exception("Flash sale not found.");

            var itemIds = flashSale.Items.Select(i => i.Id).ToList();

            // Lấy danh sách Reservation thuộc FlashSale
            var reservations = await _dbContext.Reservations
                .AsNoTracking()
                .Where(r => itemIds.Contains(r.FlashSaleItemId))
                .ToListAsync(ct);

            var totalParticipants = reservations.Select(r => r.UserId).Distinct().Count();

            // Tính tỉ lệ chuyển đổi: (Số đơn hàng Confirmed) / (Tổng số Reservation)
            var totalReservations = reservations.Count;
            var confirmedOrderIds = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.ReservationId != null && o.Status == OrderStatus.Confirmed)
                .Select(o => o.ReservationId)
                .ToListAsync(ct);
            
            var confirmedCount = reservations.Count(r => confirmedOrderIds.Contains(r.Id));

            double conversionRate = totalReservations > 0 ? (double)confirmedCount / totalReservations : 0;

            var stats = new FlashSaleStatsDto
            {
                TotalViews = 0, // TODO: Cần có hệ thống Tracking View
                TotalParticipants = totalParticipants,
                ConversionRate = conversionRate
            };

            // Fetch Product Names
            var productIds = flashSale.Items.Select(i => i.ProductId).ToList();
            var products = await _dbContext.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

            foreach (var item in flashSale.Items)
            {
                var itemName = products.ContainsKey(item.ProductId) ? products[item.ProductId] : "Unknown Product";
                
                // Lấy Stock từ Redis cho Item này (nếu đã active) hoặc fallback
                var currentStock = await _flashSaleStockCache.GetCurrentStockAsync(item.Id, ct);
                var remaining = currentStock ?? item.SaleStock;

                stats.Items.Add(new FlashSaleItemStatDto
                {
                    FlashSaleItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = itemName,
                    SaleStock = item.SaleStock,
                    RemainingStock = remaining,
                    SoldCount = item.SaleStock - remaining
                });
            }

            return stats;
        }

        public async Task<List<RevenueByDateDto>> GetRevenueChartAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
        {
            var revenues = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Confirmed && o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueByDateDto
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync(ct);

            return revenues;
        }

        public async Task<PagedResult<OrderAdminDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken ct = default)
        {
            var query = _dbContext.Orders.AsNoTracking().AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);
                
            if (filter.Type.HasValue)
                query = query.Where(o => o.Type == filter.Type.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

            var totalCount = await query.CountAsync(ct);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(o => new OrderAdminDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    OrderType = o.Type.ToString(),
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    PaymentDeadline = o.PaymentDeadline,
                    ReservationId = o.ReservationId
                })
                .ToListAsync(ct);

            return new PagedResult<OrderAdminDto>
            {
                Items = orders,
                TotalCount = totalCount,
                Page = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");

            if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                // Gọi method domain để Validate và Đổi trạng thái
                order.Cancel();

                // NẾU LÀ ĐƠN FLASH SALE: Cần HOÀN KHO REDIS! (CỰC KỲ QUAN TRỌNG)
                if (order.Type == OrderType.FlashSale && order.ReservationId.HasValue)
                {
                    var reservation = await _reservationRepository.GetByIdAsync(order.ReservationId.Value);
                    if (reservation != null)
                    {
                        // Hoàn lại kho Redis
                        await _flashSaleStockCache.IncrementStockAsync(reservation.FlashSaleItemId, reservation.Quantity, ct);
                        
                        // Đổi trạng thái Reservation thành Expired (hoặc logic domain tương ứng)
                        if (reservation.Status != ReservationStatus.Expired)
                        {
                            reservation.Expire();
                        }
                    }
                }
            }
            else if (newStatus == OrderStatus.Confirmed && order.Status != OrderStatus.Confirmed)
            {
                order.Confirm();
                
                if (order.Type == OrderType.FlashSale && order.ReservationId.HasValue)
                {
                    var reservation = await _reservationRepository.GetByIdAsync(order.ReservationId.Value);
                    if (reservation != null && reservation.Status == ReservationStatus.Holding)
                    {
                        reservation.ConvertToOrder(order.Id);
                    }
                }
            }
            else if (newStatus == OrderStatus.Completed && order.Status == OrderStatus.Confirmed)
            {
                // Dùng reflection để force set nếu Domain model không có hàm Complete() 
                // (Vì đồ án có thể chưa thiết kế hàm Complete)
                var prop = typeof(Order).GetProperty(nameof(Order.Status));
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(order, OrderStatus.Completed);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }
    }
}
