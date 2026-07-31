using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.Orders;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);
            builder.Property(o => o.ReceiverName).HasMaxLength(100).IsRequired();
            builder.Property(o => o.ReceiverPhone).HasMaxLength(20).IsRequired();
            builder.Property(o => o.ShippingAddress).HasMaxLength(500).IsRequired();
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            
            builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasMany(o => o.OrderItems)
                   .WithOne()
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Optimistic Concurrency Control
            builder.Property(o => o.RowVersion)
                   .IsRowVersion();
        }
    }

    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Price).HasColumnType("decimal(18,2)");
        }
    }
}
