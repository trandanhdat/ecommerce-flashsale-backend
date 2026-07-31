using System;

namespace FlashSale.Application.Services.Admin.DTOs
{
    public class CreateBannerDto
    {
        public string Title { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? LinkUrl { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
