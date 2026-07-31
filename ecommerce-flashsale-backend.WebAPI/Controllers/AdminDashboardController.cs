using System;
using System.Threading.Tasks;
using FlashSale.Application.Services.Admin;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.WebAPI.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Tags("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public AdminDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("flash-sales/{id}/stats")]
        public async Task<IActionResult> GetFlashSaleStats(Guid id)
        {
            try
            {
                var stats = await _dashboardService.GetFlashSaleStatsAsync(id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var revenues = await _dashboardService.GetRevenueChartAsync(fromDate, toDate);
                return Ok(revenues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderFilterDto filter)
        {
            try
            {
                var orders = await _dashboardService.GetOrdersAsync(filter);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] OrderStatus newStatus)
        {
            try
            {
                var success = await _dashboardService.UpdateOrderStatusAsync(id, newStatus);
                return Ok(new { success, message = "Cập nhật trạng thái đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
