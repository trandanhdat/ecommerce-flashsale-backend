using System.ComponentModel.DataAnnotations;

namespace FlashSale.Application.Services.Auth.DTOs
{
    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = default!;
    }
}
