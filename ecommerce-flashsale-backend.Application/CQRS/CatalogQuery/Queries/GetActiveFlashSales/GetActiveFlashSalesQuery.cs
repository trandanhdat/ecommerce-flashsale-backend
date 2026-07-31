using System.Collections.Generic;
using FlashSale.Application.Common.DTOs;
using MediatR;

namespace FlashSale.Application.CQRS.CatalogQuery.Queries.GetActiveFlashSales
{
    public class GetActiveFlashSalesQuery : IRequest<List<ActiveFlashSaleDto>>
    {
    }
}
