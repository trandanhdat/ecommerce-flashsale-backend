using System;
using System.Threading.Tasks;
using FlashSale.Application.Services.Cart;
using FlashSale.Application.Services.Cart.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var cart = await _cartService.GetMyCartAsync();
            return Ok(cart);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
        {
            try
            {
                await _cartService.AddItemAsync(dto);
                return Ok(new { message = "Thêm vào giỏ hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] int quantity)
        {
            try
            {
                await _cartService.UpdateQuantityAsync(id, quantity);
                return Ok(new { message = "Cập nhật số lượng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("items/{id}")]
        public async Task<IActionResult> RemoveItem(Guid id)
        {
            try
            {
                await _cartService.RemoveItemAsync(id);
                return Ok(new { message = "Xoá sản phẩm khỏi giỏ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                await _cartService.ClearCartAsync();
                return Ok(new { message = "Làm sạch giỏ hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var orderId = await _cartService.CheckoutAsync();
                return Ok(new { orderId, message = "Tạo đơn hàng thành công, vui lòng thanh toán." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
