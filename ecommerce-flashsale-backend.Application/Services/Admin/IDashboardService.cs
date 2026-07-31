using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Orders;

namespace FlashSale.Application.Services.Admin
{
    public interface IDashboardService
    {
        Task<FlashSaleStatsDto> GetFlashSaleStatsAsync(Guid flashSaleId, CancellationToken ct = default);
        Task<List<RevenueByDateDto>> GetRevenueChartAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
        Task<PagedResult<OrderAdminDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken ct = default);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default);
    }
}
