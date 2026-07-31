using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.Catalog;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.Property(c => c.Description).HasMaxLength(500);

            builder.HasMany(c => c.Products)
                   .WithOne(p => p.Category)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.SKU).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.ImageUrl).HasMaxLength(500);

            builder.OwnsOne(p => p.BasePrice, price =>
            {
                price.Property(m => m.Amount).HasColumnName("BasePrice").HasColumnType("decimal(18,2)");
                price.Property(m => m.Currency).HasColumnName("BasePriceCurrency").HasMaxLength(10);
            });

            builder.OwnsOne(p => p.DiscountPrice, price =>
            {
                price.Property(m => m.Amount).HasColumnName("DiscountPrice").HasColumnType("decimal(18,2)");
                price.Property(m => m.Currency).HasColumnName("DiscountPriceCurrency").HasMaxLength(10);
            });

            builder.HasIndex(p => p.SKU).IsUnique();

            builder.Property(p => p.RowVersion).IsRowVersion();
        }
    }

    public class BannerConfiguration : IEntityTypeConfiguration<Banner>
    {
        public void Configure(EntityTypeBuilder<Banner> builder)
        {
            builder.ToTable("Banners");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
            builder.Property(b => b.ImageUrl).HasMaxLength(500).IsRequired();
            builder.Property(b => b.LinkUrl).HasMaxLength(500);
        }
    }
}
