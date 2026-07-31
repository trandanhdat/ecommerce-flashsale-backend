using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Catalog
{
    public interface ICategoryRepository : IRepository<Category>
    {
        System.Threading.Tasks.Task<Category> GetBySlugAsync(string slug);
        System.Threading.Tasks.Task<bool> ExistsBySlugAsync(string slug);
    }
    
    public interface IProductRepository : IRepository<Product>
    {
        System.Threading.Tasks.Task<(System.Collections.Generic.IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(Guid? categoryId, string search, int page, int pageSize, System.Threading.CancellationToken ct = default);
        System.Threading.Tasks.Task<bool> ExistsByNameAsync(string name, System.Threading.CancellationToken ct = default);
        System.Threading.Tasks.Task<bool> ExistsBySkuAsync(string sku, System.Threading.CancellationToken ct = default);
    }
    
    public interface IBannerRepository : IRepository<Banner>
    {
        System.Threading.Tasks.Task<(System.Collections.Generic.IEnumerable<Banner> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? isActive, System.Threading.CancellationToken ct = default);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Banner>> GetActiveOrderedAsync(System.Threading.CancellationToken ct = default);
    }
}
