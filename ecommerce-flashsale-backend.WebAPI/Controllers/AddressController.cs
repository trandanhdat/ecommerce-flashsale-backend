using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.UserProfile;
using FlashSale.Application.Services.UserProfile.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce_flashsale_backend.Controllers
{
    [Route("api/addresses")]
    [ApiController]
    [Authorize] // Chỉ dành cho user đã đăng nhập
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses(CancellationToken ct = default)
        {
            var result = await _addressService.GetMyAddressesAsync(ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
        {
            var result = await _addressService.GetByIdAsync(id, ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressDto request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _addressService.CreateAsync(request, ct);
            return Ok(new { message = "Thêm địa chỉ thành công.", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddressDto request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _addressService.UpdateAsync(id, request, ct);
            return Ok(new { message = "Cập nhật địa chỉ thành công.", data = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            await _addressService.DeleteAsync(id, ct);
            return Ok(new { message = "Xóa địa chỉ thành công." });
        }

        [HttpPatch("{id}/set-default")]
        public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct = default)
        {
            await _addressService.SetDefaultAsync(id, ct);
            return Ok(new { message = "Đặt địa chỉ mặc định thành công." });
        }
    }
}
