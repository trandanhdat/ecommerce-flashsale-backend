using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.SyncFlashSaleStockToDb
{
    public class SyncFlashSaleStockToDbCommandHandler : IRequestHandler<SyncFlashSaleStockToDbCommand>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IFlashSaleStockCache _flashSaleStockCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SyncFlashSaleStockToDbCommandHandler> _logger;

        public SyncFlashSaleStockToDbCommandHandler(
            IFlashSaleRepository flashSaleRepository,
            IFlashSaleStockCache flashSaleStockCache,
            IUnitOfWork unitOfWork,
            ILogger<SyncFlashSaleStockToDbCommandHandler> logger)
        {
            _flashSaleRepository = flashSaleRepository;
            _flashSaleStockCache = flashSaleStockCache;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(SyncFlashSaleStockToDbCommand request, CancellationToken cancellationToken)
        {
            // Cơ chế "Redis giữ real-time, SQL Server là source of truth đối soát cuối"
            
            // a) Lấy tất cả FlashSale đang Active
            var activeSales = await _flashSaleRepository.GetActiveWithItemsAsync(cancellationToken);

            if (!activeSales.Any())
            {
                _logger.LogInformation("Đã quét xong. Hiện tại không có đợt Flash Sale nào đang Active để đồng bộ.");
                return;
            }

            int totalUpdated = 0;
            foreach (var sale in activeSales)
            {
                foreach (var item in sale.Items)
                {
                    // b) Gọi GetCurrentStockAsync từ Redis
                    var currentStock = await _flashSaleStockCache.GetCurrentStockAsync(item.Id, cancellationToken);
                    if (currentStock.HasValue)
                    {
                        // Tính SoldCount = SaleStock - stockCònLại
                        var soldCount = item.SaleStock - currentStock.Value;
                        
                        // c) Cập nhật FlashSaleItem.SoldCount trong DB
                        item.UpdateSoldCount(soldCount);

                        try
                        {
                            // Lưu từng phần để catch RowVersion conflict (Optimistic Concurrency)
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("✅ [Sync] Đã đồng bộ: Sản phẩm {ItemId} | Đã bán: {SoldCount} | Còn lại: {CurrentStock}", item.Id, soldCount, currentStock.Value);
                            totalUpdated++;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            // Catch DbUpdateConcurrencyException và log warning, KHÔNG throw crash cả batch
                            _logger.LogWarning(ex, "Xung đột dữ liệu khi cập nhật SoldCount cho FlashSaleItem {ItemId}. Bỏ qua và đi tiếp.", item.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi hệ thống khi lưu đồng bộ Stock cho Item {ItemId}.", item.Id);
                        }
                    }
                }
            }
            
            _logger.LogInformation("Đã đồng bộ xong số liệu bán cho {Count} sản phẩm Flash Sale.", totalUpdated);
        }
    }
}
