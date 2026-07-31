using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Common.Mappers;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using Microsoft.Extensions.Logging;

namespace FlashSale.Infrastructure.Caching
{
    public class ProductCatalogCacheWarmer : IProductCatalogCacheWarmer
    {
        private readonly IProductRepository _productRepository;
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IProductCatalogCache _cache;
        private readonly ILogger<ProductCatalogCacheWarmer> _logger;

        public ProductCatalogCacheWarmer(
            IProductRepository productRepository,
            IFlashSaleRepository flashSaleRepository,
            IProductCatalogCache cache,
            ILogger<ProductCatalogCacheWarmer> logger)
        {
            _productRepository = productRepository;
            _flashSaleRepository = flashSaleRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task WarmAsync(Guid productId, CancellationToken ct = default)
        {
            _logger.LogInformation("Warming cache for ProductId: {ProductId}", productId);

            // 1. Lấy Product gốc từ DB
            var product = await _productRepository.GetByIdAsync(productId);
            
            if (product == null)
            {
                // Nếu Product không còn tồn tại (bị xoá) => Xóa cache lẻ
                await _cache.RemoveProductAsync(productId, ct);
                _logger.LogInformation("Product {ProductId} not found. Removed from cache.", productId);
            }
            else
            {
                // 2. Kiểm tra xem có đang nằm trong FlashSale Active không
                var activeFlashSales = await _flashSaleRepository.GetActiveWithItemsAsync(ct);
                var fsItem = activeFlashSales.SelectMany(f => f.Items).FirstOrDefault(i => i.ProductId == productId);

                // 3. Dùng Mapper để sinh DTO
                var dto = product.ToProductCatalogCacheDto(fsItem);

                // 4. Ghi đè cache lẻ
                await _cache.SetProductAsync(dto, null, ct);
                _logger.LogInformation("Product {ProductId} cache warmed successfully.", productId);
            }

            // 5. Luôn gọi InvalidateProductListCacheAsync
            // Lý do: Các trang danh sách (List DTOs) rất phức tạp vì phụ thuộc vào pagination, search filter, category filter.
            // Khi 1 sản phẩm thay đổi thông tin hoặc bị xoá, ta không thể tìm và cập nhật chính xác nó trong từng key list cache được.
            // Do đó, xóa toàn bộ key 'product:list:*' là phương án an toàn nhất và dễ bảo trì nhất, 
            // đảm bảo người dùng luôn thấy list mới nhất ở lần query tiếp theo (chấp nhận lần đó sẽ Cache Miss và tốn chi phí query DB).
            await _cache.InvalidateProductListCacheAsync(ct);
            _logger.LogInformation("Invalidated all product list caches due to update on ProductId: {ProductId}", productId);
        }
    }
}
