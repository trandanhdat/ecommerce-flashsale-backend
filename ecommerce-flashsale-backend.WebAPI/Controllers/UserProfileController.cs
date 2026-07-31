using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.UserProfile;
using FlashSale.Application.Services.UserProfile.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce_flashsale_backend.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize] // Bất kỳ user nào đăng nhập (kể cả User thường hay Admin) đều được phép gọi
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct = default)
        {
            var result = await _userProfileService.GetMyProfileAsync(ct);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto request, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userProfileService.UpdateMyProfileAsync(request, ct);
            return Ok(new { message = "Cập nhật hồ sơ thành công.", data = result });
        }
    }
}
