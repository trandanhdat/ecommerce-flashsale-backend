using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.DTOs;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IProductCatalogCache
    {
        Task<ProductCatalogCacheDto?> GetProductAsync(Guid productId, CancellationToken ct = default);
        Task SetProductAsync(ProductCatalogCacheDto dto, TimeSpan? ttl = null, CancellationToken ct = default);
        Task RemoveProductAsync(Guid productId, CancellationToken ct = default);

        Task<IEnumerable<ProductCatalogCacheDto>?> GetProductListAsync(int page, int pageSize, Guid? categoryId, CancellationToken ct = default);
        Task SetProductListAsync(int page, int pageSize, Guid? categoryId, IEnumerable<ProductCatalogCacheDto> items, TimeSpan? ttl = null, CancellationToken ct = default);
        
        Task InvalidateProductListCacheAsync(CancellationToken ct = default);
    }
}
