using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlashSale.Domain.Reservations;

namespace FlashSale.Infrastructure.Persistence.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("Reservations");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Optimistic Concurrency Control
            builder.Property(r => r.RowVersion)
                   .IsRowVersion();
        }
    }
}
