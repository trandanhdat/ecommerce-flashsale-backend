using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : RepositoryBase<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(Guid? categoryId, string search, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _dbContext.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        {
            return await _dbContext.Products.AnyAsync(p => p.Name == name, ct);
        }

        public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
        {
            return await _dbContext.Products.AnyAsync(p => p.SKU == sku, ct);
        }
    }
}
