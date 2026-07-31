using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Catalog;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class BannerRepository : RepositoryBase<Banner>, IBannerRepository
    {
        public BannerRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<(IEnumerable<Banner> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(b => b.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(b => b.DisplayOrder)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<IEnumerable<Banner>> GetActiveOrderedAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            
            return await _dbSet.AsNoTracking()
                .Where(b => b.IsActive && 
                            (!b.StartDate.HasValue || b.StartDate <= now) && 
                            (!b.EndDate.HasValue || b.EndDate >= now))
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync(ct);
        }
    }
}
