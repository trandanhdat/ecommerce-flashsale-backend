using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.Catalog.ValueObjects;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Application.Services.Admin
{
    public class ProductAdminService : IProductAdminService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IProductCatalogCacheWarmer _cacheWarmer;
        private readonly IMapper _mapper;

        public ProductAdminService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IFlashSaleRepository flashSaleRepository,
            IProductCatalogCacheWarmer cacheWarmer,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _flashSaleRepository = flashSaleRepository;
            _cacheWarmer = cacheWarmer;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductAdminDto>> GetAllAsync(Guid? categoryId, string search, int page, int pageSize, CancellationToken ct = default)
        {
            var (items, totalCount) = await _productRepository.GetPagedAsync(categoryId, search, page, pageSize, ct);
            var dtos = _mapper.Map<IEnumerable<ProductAdminDto>>(items);
            
            return new PagedResult<ProductAdminDto>
            {
                Items = System.Linq.Enumerable.ToList(dtos),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ProductAdminDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                throw new DomainException("Không tìm thấy sản phẩm.");
            }

            return _mapper.Map<ProductAdminDto>(product);
        }

        public async Task<ProductAdminDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            // Validate CategoryId tồn tại
            var categoryExists = await _categoryRepository.GetByIdAsync(dto.CategoryId) != null;
            if (!categoryExists)
            {
                throw new DomainException("Danh mục không tồn tại.");
            }

            // Validate SKU duy nhất
            if (await _productRepository.ExistsBySkuAsync(dto.SKU, ct))
            {
                throw new DomainException($"Mã sản phẩm (SKU) '{dto.SKU}' đã tồn tại.");
            }

            var product = new Product(
                dto.CategoryId,
                dto.SKU,
                dto.Name,
                dto.Description,
                dto.ImageUrl,
                new Money(dto.BasePrice, "VND"),
                dto.StockQuantity
            );

            await _productRepository.AddAsync(product);

            // Nối dây sẵn cho Phase 5: Warm Cache
            await _cacheWarmer.WarmAsync(product.Id, ct);

            return _mapper.Map<ProductAdminDto>(product);
        }

        public async Task UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                throw new DomainException("Không tìm thấy sản phẩm.");
            }

            // Validate CategoryId tồn tại
            var categoryExists = await _categoryRepository.GetByIdAsync(dto.CategoryId) != null;
            if (!categoryExists)
            {
                throw new DomainException("Danh mục không tồn tại.");
            }

            if (product.SKU != dto.SKU)
            {
                if (await _productRepository.ExistsBySkuAsync(dto.SKU, ct))
                {
                    throw new DomainException($"Mã sản phẩm (SKU) '{dto.SKU}' đã tồn tại.");
                }
            }

            product.Update(
                dto.CategoryId,
                dto.SKU,
                dto.Name,
                dto.Description,
                dto.ImageUrl,
                new Money(dto.BasePrice, "VND")
            );

            // Cập nhật Stock nếu cần (ví dụ đơn giản, nếu khác thì adjust)
            if (dto.StockQuantity != product.StockQuantity)
            {
                var diff = dto.StockQuantity - product.StockQuantity;
                product.AdjustStock(diff);
            }

            await _productRepository.UpdateAsync(product);

            await _cacheWarmer.WarmAsync(product.Id, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                throw new DomainException("Không tìm thấy sản phẩm.");
            }

            // Kiểm tra không cho xoá nếu Product đang gắn với FlashSaleItem
            var isInFlashSale = await _flashSaleRepository.HasProductAsync(id, ct);
            if (isInFlashSale)
            {
                throw new DomainException("Không thể xoá sản phẩm đang nằm trong chương trình Flash Sale.");
            }

            await _productRepository.DeleteAsync(product);

            // Cập nhật Cache (xóa cache hoặc thông báo thay đổi)
            // Tạm thời gọi WarmAsync để log, tuy nhiên thực tế có thể cần hàm RemoveCacheAsync
            await _cacheWarmer.WarmAsync(product.Id, ct);
        }
    }
}
