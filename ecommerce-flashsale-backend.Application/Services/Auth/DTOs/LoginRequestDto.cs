using System.ComponentModel.DataAnnotations;

namespace FlashSale.Application.Services.Auth.DTOs
{
    public class LoginRequestDto
    {
        /// <summary>
        /// Địa chỉ email đăng nhập của người dùng.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        /// <summary>
        /// Mật khẩu đăng nhập.
        /// </summary>
        [Required]
        public string Password { get; set; } = default!;
    }
}
