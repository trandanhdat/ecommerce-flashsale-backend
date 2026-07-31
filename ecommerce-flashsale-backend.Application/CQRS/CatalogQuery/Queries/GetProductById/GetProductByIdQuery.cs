using System;
using FlashSale.Application.Common.DTOs;
using MediatR;

namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetProductById
{
    public class GetProductByIdQuery : IRequest<ProductCatalogCacheDto?>
    {
        public Guid Id { get; set; }
    }
}
