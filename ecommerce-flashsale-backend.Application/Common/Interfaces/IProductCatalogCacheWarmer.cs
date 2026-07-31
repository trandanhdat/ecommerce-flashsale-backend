using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IProductCatalogCacheWarmer
    {
        Task WarmAsync(Guid productId, CancellationToken ct = default);
    }
}
