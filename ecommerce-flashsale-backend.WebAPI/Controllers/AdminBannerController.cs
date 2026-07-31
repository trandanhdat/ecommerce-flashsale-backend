using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.Admin;
using FlashSale.Application.Services.Admin.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce_flashsale_backend.Controllers
{
    [Route("api/admin/banners")]
    [ApiController]
    [Tags("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminBannerController : ControllerBase
    {
        private readonly IBannerService _bannerService;

        public AdminBannerController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBannerDto request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _bannerService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _bannerService.GetAllAsync(page, pageSize, isActive, ct);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
        {
            var result = await _bannerService.GetByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBannerDto request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _bannerService.UpdateAsync(id, request, ct);
            return Ok(new { message = "Cập nhật banner thành công." });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            await _bannerService.DeleteAsync(id, ct);
            return Ok(new { message = "Xóa banner thành công." });
        }

        [HttpPatch("{id:guid}/toggle-active")]
        public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct = default)
        {
            await _bannerService.ToggleActiveAsync(id, ct);
            return Ok(new { message = "Thay đổi trạng thái banner thành công." });
        }
    }
}
