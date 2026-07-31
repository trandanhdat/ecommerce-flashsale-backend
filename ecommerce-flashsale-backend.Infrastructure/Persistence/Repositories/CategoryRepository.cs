using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Catalog;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<Category> GetBySlugAsync(string slug)
        {
            return await _dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public new async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Category>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<bool> ExistsBySlugAsync(string slug)
        {
            return await _dbContext.Categories.AnyAsync(c => c.Slug == slug);
        }
    }
}
