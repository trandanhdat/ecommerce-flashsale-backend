using System.Threading.Tasks;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.WebAPI.Controllers
{
    [Route("api/flash-sale-orders")]
    [ApiController]
    [Tags("FlashSaleOrders")]
    [Authorize]
    public class FlashSaleOrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FlashSaleOrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [AllowAnonymous] // Bật AllowAnonymous để chạy test K6 không cần tạo 500 Token
        [EnableRateLimiting("flashsale-order-policy")]
        public async Task<IActionResult> PlaceFlashSaleOrder([FromBody] PlaceFlashSaleOrderCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                return Ok(new {
                    result.OrderId,
                    result.ReservationId,
                    Message = "Đặt hàng thành công."
                });
            }

            return result.ErrorCode switch
            {
                PlaceFlashSaleOrderErrorCode.SaleNotActive => Conflict(new { Message = result.ErrorMessage }),
                PlaceFlashSaleOrderErrorCode.StockNotInitialized => Conflict(new { Message = result.ErrorMessage }),
                PlaceFlashSaleOrderErrorCode.InsufficientStock => Conflict(new { Message = result.ErrorMessage }),
                PlaceFlashSaleOrderErrorCode.AlreadyHasPendingReservation => Conflict(new { Message = result.ErrorMessage }),
                PlaceFlashSaleOrderErrorCode.ExceedMaxQuantityPerUser => BadRequest(new { Message = result.ErrorMessage }),
                _ => BadRequest(new { Message = result.ErrorMessage })
            };
        }

        [HttpPost("{flashSaleItemId}/add-stock")]
        [Authorize(Roles = "Admin")] // Đã bật phân quyền: Chỉ Admin mới được sửa
        public async Task<IActionResult> AddStock(Guid flashSaleItemId, [FromBody] int quantityToAdd)
        {
            var command = new FlashSale.Application.CQRS.FlashSaleOrders.Commands.UpdateFlashSaleStock.UpdateFlashSaleStockCommand(flashSaleItemId, quantityToAdd);
            var success = await _mediator.Send(command);

            if (success)
            {
                return Ok(new { Message = $"Đã bơm thêm {quantityToAdd} sản phẩm vào kho SQL và Redis thành công!" });
            }

            return BadRequest(new { Message = "Không tìm thấy FlashSaleItem hoặc đợt Sale chưa diễn ra." });
        }
    }
}
