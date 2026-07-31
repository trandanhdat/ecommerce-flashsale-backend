using System.ComponentModel.DataAnnotations;

namespace FlashSale.Application.Services.Auth.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        public string FullName { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = default!;
    }
}
