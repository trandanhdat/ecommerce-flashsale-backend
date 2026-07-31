using System.Linq;
using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Orders;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Order>> GetPendingOrdersByReservationIdsAsync(System.Collections.Generic.IEnumerable<System.Guid> reservationIds, System.Threading.CancellationToken ct = default)
        {
            return await _dbSet
                .Where(o => o.ReservationId.HasValue && reservationIds.Contains(o.ReservationId.Value) && o.Status == OrderStatus.Pending)
                .ToListAsync(ct);
        }
    }
}
