using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.Users;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(i => i.Id);

            // Cấu hình Index để query nhanh
            builder.HasIndex(i => i.UserId);
            builder.HasIndex(i => new { i.UserId, i.ProductId }).IsUnique(); // Một user chỉ có 1 dòng cho mỗi sản phẩm

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(i => i.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<FlashSale.Domain.Catalog.Product>()
                   .WithMany()
                   .HasForeignKey(i => i.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
