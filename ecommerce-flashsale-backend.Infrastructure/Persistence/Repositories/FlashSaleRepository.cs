using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.FlashSales;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class FlashSaleRepository : RepositoryBase<FlashSale.Domain.FlashSales.FlashSale>, IFlashSaleRepository
    {
        public FlashSaleRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<bool> HasProductAsync(Guid productId, CancellationToken ct = default)
        {
            return await _dbContext.FlashSales
                .Include(f => f.Items)
                .AnyAsync(f => f.Items.Any(i => i.ProductId == productId), ct);
        }

        public async Task<IEnumerable<FlashSale.Domain.FlashSales.FlashSale>> GetActiveWithItemsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return await _dbContext.FlashSales
                .Include(f => f.Items)
                .Where(f => f.Status == FlashSaleStatus.Active && f.StartTime <= now && f.EndTime >= now)
                .ToListAsync(ct);
        }

        public async Task<FlashSaleItem?> GetActiveItemByIdAsync(Guid flashSaleItemId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var flashSale = await _dbContext.FlashSales
                .Include(f => f.Items)
                .Where(f => f.Status == FlashSaleStatus.Active && f.StartTime <= now && f.EndTime >= now)
                .FirstOrDefaultAsync(f => f.Items.Any(i => i.Id == flashSaleItemId), ct);

            return flashSale?.Items.FirstOrDefault(i => i.Id == flashSaleItemId);
        }

        public async Task<FlashSale.Domain.FlashSales.FlashSale?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.FlashSales
                .Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.Id == id, ct);
        }
    }
}
