using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Orders
{
    public interface IOrderRepository : IRepository<Order>
    {
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Order>> GetPendingOrdersByReservationIdsAsync(System.Collections.Generic.IEnumerable<System.Guid> reservationIds, System.Threading.CancellationToken ct = default);
    }
}
