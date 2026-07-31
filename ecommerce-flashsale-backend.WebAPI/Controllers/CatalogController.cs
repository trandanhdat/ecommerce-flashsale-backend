using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.CQRS.CatalogQuery.Queries.GetActiveFlashSales;
using FlashSale.Application.CQRS.CatalogQuery.Queries.GetProductById;
using FlashSale.Application.CQRS.CatalogQuery.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce_flashsale_backend.Controllers
{
    // API CÔNG KHAI - Dành cho khách hàng xem sản phẩm (Không dùng [Authorize])
    [Route("api/catalog")]
    [ApiController]
    [Tags("Catalog")]
    [AllowAnonymous]
    public class CatalogController : ControllerBase
    {
        private readonly IMediator _mediator;

        // Lưu ý: Kiến trúc CQRS sử dụng trực tiếp IMediator thay vì IProductAdminService
        public CatalogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? categoryId = null, [FromQuery] string? search = null, CancellationToken ct = default)
        {
            var query = new GetProductsQuery
            {
                Page = page,
                PageSize = pageSize,
                CategoryId = categoryId,
                Search = search
            };

            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct = default)
        {
            var query = new GetProductByIdQuery { Id = id };
            var result = await _mediator.Send(query, ct);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(result);
        }

        [HttpGet("flash-sales/active")]
        public async Task<IActionResult> GetActiveFlashSales(CancellationToken ct = default)
        {
            var query = new GetActiveFlashSalesQuery();
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }
    }
}
