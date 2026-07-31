using FlashSale.Domain.Payments;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : RepositoryBase<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<Payment> GetPendingPaymentByOrderIdAsync(System.Guid orderId, CancellationToken ct = default)
        {
            return _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending, ct);
        }

        public Task<Payment> GetByTransactionNoAsync(string transactionNo, CancellationToken ct = default)
        {
            return _dbSet.FirstOrDefaultAsync(p => p.TransactionNo == transactionNo, ct);
        }

        public void Delete(Payment payment)
        {
            _dbSet.Remove(payment);
        }
    }
}
