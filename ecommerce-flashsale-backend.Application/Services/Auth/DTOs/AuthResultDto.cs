using System;

namespace FlashSale.Application.Services.Auth.DTOs
{
    public class AuthResultDto
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
