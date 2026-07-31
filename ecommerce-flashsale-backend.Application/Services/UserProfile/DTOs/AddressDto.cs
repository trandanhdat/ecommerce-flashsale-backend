using System;

namespace FlashSale.Application.Services.UserProfile.DTOs
{
    public class AddressDto
    {
        public Guid Id { get; set; }
        public string RecipientName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string DetailAddress { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}
