using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Enums;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IFlashSaleStockCache
    {
        Task<bool> InitStockAsync(Guid flashSaleItemId, int saleStock, CancellationToken ct = default);
        Task<StockDecrementResult> TryDecrementStockAsync(Guid flashSaleItemId, int quantity, CancellationToken ct = default);
        Task<int> IncrementStockAsync(Guid flashSaleItemId, int quantity, CancellationToken ct = default);
        Task<int?> GetCurrentStockAsync(Guid flashSaleItemId, CancellationToken ct = default);
    }
}
