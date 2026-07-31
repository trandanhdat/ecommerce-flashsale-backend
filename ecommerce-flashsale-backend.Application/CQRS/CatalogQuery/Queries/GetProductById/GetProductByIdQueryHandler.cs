using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.DTOs;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Common.Mappers;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using MediatR;
using Microsoft.Extensions.Logging;
using FlashSale.Domain.Catalog.ValueObjects;
namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetProductById
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductCatalogCacheDto?>
    {
        private readonly IProductCatalogCache _cache;
        private readonly IProductRepository _productRepository;
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly ILogger<GetProductByIdQueryHandler> _logger;

        public GetProductByIdQueryHandler(
            IProductCatalogCache cache,
            IProductRepository productRepository,
            IFlashSaleRepository flashSaleRepository,
            ILogger<GetProductByIdQueryHandler> logger)
        {
            _cache = cache;
            _productRepository = productRepository;
            _flashSaleRepository = flashSaleRepository;
            _logger = logger;
        }

        public async Task<ProductCatalogCacheDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Cache
            var cachedItem = await _cache.GetProductAsync(request.Id, cancellationToken);
            if (cachedItem != null)
            {
                _logger.LogInformation("CACHE HIT: Đã lấy sản phẩm {ProductId} từ Redis.", request.Id);
                return cachedItem;
            }

            _logger.LogInformation("CACHE MISS: Query database để lấy sản phẩm {ProductId}.", request.Id);

            // 2. Query DB nếu Miss
            var product = await _productRepository.GetByIdAsync(request.Id);
            if (product == null)
                return null;

            // Lấy FlashSale
            var activeFlashSales = await _flashSaleRepository.GetActiveWithItemsAsync(cancellationToken);
            var fsItem = activeFlashSales.SelectMany(f => f.Items).FirstOrDefault(i => i.ProductId == product.Id);
            
            var dto = product.ToProductCatalogCacheDto(fsItem);

            // 3. Set Cache
            await _cache.SetProductAsync(dto, null, cancellationToken);

            return dto;
        }
    }
}
