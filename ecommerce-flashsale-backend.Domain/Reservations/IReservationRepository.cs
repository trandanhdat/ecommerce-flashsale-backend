using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Reservations
{
    public interface IReservationRepository : IRepository<Reservation>
    {
        Task<Reservation?> GetHoldingByUserAndItemAsync(Guid userId, Guid flashSaleItemId, System.Threading.CancellationToken ct = default);
        Task<System.Collections.Generic.IEnumerable<Reservation>> GetExpiredHoldingsAsync(System.DateTime now, System.Threading.CancellationToken ct = default);
    }
}
