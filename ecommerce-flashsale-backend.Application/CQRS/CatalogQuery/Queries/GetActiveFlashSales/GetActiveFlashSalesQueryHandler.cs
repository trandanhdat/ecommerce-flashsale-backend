using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.DTOs;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using MediatR;

namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetActiveFlashSales
{
    public class GetActiveFlashSalesQueryHandler : IRequestHandler<GetActiveFlashSalesQuery, List<ActiveFlashSaleDto>>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IProductRepository _productRepository;

        public GetActiveFlashSalesQueryHandler(IFlashSaleRepository flashSaleRepository, IProductRepository productRepository)
        {
            _flashSaleRepository = flashSaleRepository;
            _productRepository = productRepository;
        }

        public async Task<List<ActiveFlashSaleDto>> Handle(GetActiveFlashSalesQuery request, CancellationToken cancellationToken)
        {
            var activeSales = await _flashSaleRepository.GetActiveWithItemsAsync(cancellationToken);
            
            // Collect all product IDs to fetch from ProductRepository
            var productIds = activeSales.SelectMany(f => f.Items).Select(i => i.ProductId).Distinct().ToList();
            
            // We use GetAllAsync then filter in memory for simplicity since it's a small dataset,
            // or we could add GetByIdsAsync to IProductRepository. For now we use GetAsync if it exists,
            // wait, we can just do GetAsync(p => productIds.Contains(p.Id)) because it's available in IRepository<T>.
            var products = await _productRepository.GetAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id, p => p);

            var result = activeSales.Select(fs => new ActiveFlashSaleDto
            {
                FlashSaleId = fs.Id,
                FlashSaleName = fs.Title, // Fixed from fs.Name
                EndTime = fs.EndTime,
                Items = fs.Items.Select(i =>
                {
                    productDict.TryGetValue(i.ProductId, out var product);
                    return new ActiveFlashSaleItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = product?.Name ?? string.Empty,
                        ImageUrl = product?.ImageUrl ?? string.Empty,
                        SalePrice = i.SalePrice.Amount,
                        SaleStock = i.SaleStock,
                        SoldCount = i.SoldCount
                    };
                }).ToList()
            }).ToList();

            return result;
        }
    }
}
