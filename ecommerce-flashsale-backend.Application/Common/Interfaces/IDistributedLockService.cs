using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IDistributedLockService
    {
        Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiration, CancellationToken ct = default);
        Task ReleaseLockAsync(string lockKey, CancellationToken ct = default);
    }
}
