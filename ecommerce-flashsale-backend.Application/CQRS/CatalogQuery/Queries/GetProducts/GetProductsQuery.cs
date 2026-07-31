using System;
using FlashSale.Application.Common.DTOs;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using MediatR;

namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetProducts
{
    public class GetProductsQuery : IRequest<PagedResult<ProductCatalogCacheDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? CategoryId { get; set; }
        public string? Search { get; set; }
    }
}
