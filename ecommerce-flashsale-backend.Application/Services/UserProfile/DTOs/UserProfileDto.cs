using System;

namespace FlashSale.Application.Services.UserProfile.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
