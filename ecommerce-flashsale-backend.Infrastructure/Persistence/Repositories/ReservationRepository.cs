using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Reservations;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class ReservationRepository : RepositoryBase<Reservation>, IReservationRepository
    {
        public ReservationRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<Reservation?> GetHoldingByUserAndItemAsync(Guid userId, Guid flashSaleItemId, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(r => 
                r.UserId == userId && 
                r.FlashSaleItemId == flashSaleItemId && 
                r.Status == ReservationStatus.Holding, ct);
        }

        public async Task<System.Collections.Generic.IEnumerable<Reservation>> GetExpiredHoldingsAsync(DateTime now, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(r => r.Status == ReservationStatus.Holding && r.ExpiresAt <= now)
                .ToListAsync(ct);
        }
    }
}
