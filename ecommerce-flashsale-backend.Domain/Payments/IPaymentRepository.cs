using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Payments
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment> GetPendingPaymentByOrderIdAsync(System.Guid orderId, System.Threading.CancellationToken ct = default);
        Task<Payment> GetByTransactionNoAsync(string transactionNo, System.Threading.CancellationToken ct = default);
        void Delete(Payment payment);
    }
}
