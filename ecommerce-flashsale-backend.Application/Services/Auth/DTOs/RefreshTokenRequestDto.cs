using System.ComponentModel.DataAnnotations;

namespace FlashSale.Application.Services.Auth.DTOs
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = default!;
    }
}
