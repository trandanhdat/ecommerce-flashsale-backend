using System.Threading.Tasks;
using FlashSale.Domain.SeedWork;
using System.Collections.Generic;
using System;
using System.Threading;
namespace FlashSale.Domain.FlashSales
{
    public interface IFlashSaleRepository : IRepository<FlashSale>
    {
        Task<bool> HasProductAsync(Guid productId, CancellationToken ct = default);
        Task<IEnumerable<FlashSale>> GetActiveWithItemsAsync(CancellationToken ct = default);
        Task<FlashSaleItem?> GetActiveItemByIdAsync(Guid flashSaleItemId, CancellationToken ct = default);
        Task<FlashSale?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    }
}
