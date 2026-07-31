using FlashSale.Application.Common.DTOs;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;

namespace FlashSale.Application.Common.Mappers
{
    public static class ProductCatalogMapper
    {
        public static ProductCatalogCacheDto ToProductCatalogCacheDto(this Product product, FlashSaleItem? fsItem = null)
        {
            var dto = new ProductCatalogCacheDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                BasePrice = product.BasePrice.Amount,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty
            };

            if (fsItem != null)
            {
                dto.HasActiveFlashSale = true;
                dto.FlashSalePrice = fsItem.SalePrice.Amount;
            }

            return dto;
        }
    }
}
