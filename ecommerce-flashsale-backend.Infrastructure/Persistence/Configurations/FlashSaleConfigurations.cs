using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.FlashSales;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale.Domain.FlashSales.FlashSale>
    {
        public void Configure(EntityTypeBuilder<FlashSale.Domain.FlashSales.FlashSale> builder)
        {
            builder.ToTable("FlashSales");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Title).HasMaxLength(200).IsRequired();
            builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasMany(f => f.Items)
                   .WithOne(i => i.FlashSale)
                   .HasForeignKey(i => i.FlashSaleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class FlashSaleItemConfiguration : IEntityTypeConfiguration<FlashSaleItem>
    {
        public void Configure(EntityTypeBuilder<FlashSaleItem> builder)
        {
            builder.ToTable("FlashSaleItems");

            builder.HasKey(i => i.Id);

            builder.OwnsOne(i => i.SalePrice, price =>
            {
                price.Property(m => m.Amount).HasColumnName("SalePrice").HasColumnType("decimal(18,2)");
                price.Property(m => m.Currency).HasColumnName("SalePriceCurrency").HasMaxLength(10);
            });

            // Optimistic Concurrency Control
            builder.Property(i => i.RowVersion)
                   .IsRowVersion();
        }
    }
}
