using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.Payments;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.TransactionNo).HasMaxLength(100);
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");

            builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(50);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            // Optimistic Concurrency Control
            builder.Property(p => p.RowVersion)
                   .IsRowVersion();
        }
    }
}
