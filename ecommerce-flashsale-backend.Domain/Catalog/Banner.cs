using System;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Catalog.Exceptions;

namespace FlashSale.Domain.Catalog
{
    public class Banner : AggregateRoot
    {
        public string Title { get; private set; }
        public string ImageUrl { get; private set; }
        public string? LinkUrl { get; private set; }
        public int DisplayOrder { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }

        protected Banner() { }

        private Banner(string title, string imageUrl, string? linkUrl, int displayOrder, DateTime? startDate, DateTime? endDate)
        {
            Id = Guid.NewGuid();
            Title = title;
            ImageUrl = imageUrl;
            LinkUrl = linkUrl;
            DisplayOrder = displayOrder;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = true;
        }

        public static Banner Create(string title, string imageUrl, string? linkUrl, int displayOrder, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề Banner không được để trống.");
                
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new DomainException("Đường dẫn hình ảnh Banner không được để trống.");

            return new Banner(title, imageUrl, linkUrl, displayOrder, startDate, endDate);
        }

        public void Update(string title, string imageUrl, string? linkUrl, int displayOrder, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề Banner không được để trống.");
                
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new DomainException("Đường dẫn hình ảnh Banner không được để trống.");

            Title = title;
            ImageUrl = imageUrl;
            LinkUrl = linkUrl;
            DisplayOrder = displayOrder;
            StartDate = startDate;
            EndDate = endDate;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
