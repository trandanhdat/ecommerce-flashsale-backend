using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.DTOs;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Common.Mappers;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetProducts
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductCatalogCacheDto>>
    {
        private readonly IProductCatalogCache _cache;
        private readonly IProductRepository _productRepository;
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly ILogger<GetProductsQueryHandler> _logger;

        public GetProductsQueryHandler(
            IProductCatalogCache cache,
            IProductRepository productRepository,
            IFlashSaleRepository flashSaleRepository,
            ILogger<GetProductsQueryHandler> logger)
        {
            _cache = cache;
            _productRepository = productRepository;
            _flashSaleRepository = flashSaleRepository;
            _logger = logger;
        }

        public async Task<PagedResult<ProductCatalogCacheDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra cache
            // Lưu ý: Nếu Search != null, ta không dùng Redis list (vì khó đoán) hoặc có thể lưu theo một key riêng biệt (trong case này ta bỏ qua search khỏi key để đơn giản hoá).
            // Tuy nhiên, theo yêu cầu, GetProductListAsync đã hỗ trợ categoryId. Ta chỉ check cache nếu không có search.
            if (string.IsNullOrWhiteSpace(request.Search))
            {
                var cachedItems = await _cache.GetProductListAsync(request.Page, request.PageSize, request.CategoryId, cancellationToken);
                if (cachedItems != null)
                {
                    _logger.LogInformation("CACHE HIT: Đã lấy danh sách sản phẩm từ Redis.");
                    // Chú ý: Ở đây ta lờ đi TotalCount (đáng nhẽ phải lưu luôn vào Cache). Nhưng để tạm thì cứ trả về.
                    return new PagedResult<ProductCatalogCacheDto>
                    {
                        Items = cachedItems.ToList(),
                        TotalCount = cachedItems.Count(),
                        Page = request.Page,
                        PageSize = request.PageSize
                    };
                }
            }

            _logger.LogInformation("CACHE MISS: Query database để lấy danh sách sản phẩm.");

            // 2. Fallback DB
            var (products, totalCount) = await _productRepository.GetPagedAsync(
                request.CategoryId,
                request.Search,
                request.Page,
                request.PageSize,
                cancellationToken);

            // Truy vấn xem các sản phẩm này có đang chạy Flash Sale không
            var activeFlashSales = await _flashSaleRepository.GetActiveWithItemsAsync(cancellationToken);
            var activeFlashSaleItems = activeFlashSales
                .SelectMany(f => f.Items)
                .ToDictionary(i => i.ProductId, i => i);

            // Build danh sách DTO
            var resultItems = products.Select(p =>
            {
                activeFlashSaleItems.TryGetValue(p.Id, out var fsItem);
                return FlashSale.Application.Common.Mappers.ProductCatalogMapper.ToProductCatalogCacheDto(p, fsItem);
            }).ToList();

            // 3. Set lại Cache nếu không có search
            if (string.IsNullOrWhiteSpace(request.Search))
            {
                await _cache.SetProductListAsync(request.Page, request.PageSize, request.CategoryId, resultItems, null, cancellationToken);
            }

            return new PagedResult<ProductCatalogCacheDto>
            {
                Items = resultItems,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
