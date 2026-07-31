using System;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Common.Guards;
using FlashSale.Domain.Catalog.ValueObjects;
using FlashSale.Domain.Catalog.Exceptions;
using FlashSale.Domain.Catalog.Events;

namespace FlashSale.Domain.Catalog
{
    public class Product : AggregateRoot
    {
        public Guid CategoryId { get; private set; }
        public string SKU { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string ImageUrl { get; private set; }
        
        public Money BasePrice { get; private set; }
        public Money DiscountPrice { get; private set; }
        
        public int StockQuantity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public byte[] RowVersion { get; private set; }

        public Category Category { get; private set; }

        protected Product() { }

        public Product(Guid categoryId, string sku, string name, string description, string imageUrl, Money basePrice, int initialStock)
        {
            Id = Guid.NewGuid();
            CategoryId = categoryId;
            SKU = sku;
            Name = name;
            Description = description;
            ImageUrl = imageUrl;
            BasePrice = basePrice;
            DiscountPrice = new Money(basePrice.Amount, basePrice.Currency); // Gán mặc định bằng giá gốc nhưng tạo instance mới để EF không bị lỗi tracking
            StockQuantity = initialStock;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void AdjustStock(int quantity)
        {
            if (StockQuantity + quantity < 0)
            {
                throw new InsufficientStockException(Id, quantity);
            }
            StockQuantity += quantity;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductStockAdjustedEvent(Id, StockQuantity));
        }

        public void Update(Guid categoryId, string sku, string name, string description, string imageUrl, Money basePrice)
        {
            CategoryId = categoryId;
            SKU = sku;
            Name = name;
            Description = description;
            ImageUrl = imageUrl;
            BasePrice = basePrice;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
