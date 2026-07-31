using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.Caching
{
    public class RedisDistributedLockService : IDistributedLockService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisDistributedLockService> _logger;
        private readonly IDatabase _db;
        private readonly string _lockValue;

        public RedisDistributedLockService(IConnectionMultiplexer redis, ILogger<RedisDistributedLockService> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
            // Use a unique value for this instance to ensure we only release locks we own.
            _lockValue = Guid.NewGuid().ToString(); 
        }

        public async Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiration, CancellationToken ct = default)
        {
            try
            {
                // LockTakeAsync is equivalent to SETNX with an expiration time.
                // It returns true if the lock was successfully acquired.
                bool acquired = await _db.LockTakeAsync(lockKey, _lockValue, expiration);
                if (acquired)
                {
                    _logger.LogDebug("Acquired distributed lock: {LockKey}", lockKey);
                }
                else
                {
                    _logger.LogWarning("Failed to acquire distributed lock: {LockKey} (Already held)", lockKey);
                }
                return acquired;
            }
            catch (Exception ex)
            {
                // If Redis is down, we can choose to fail open or fail closed.
                // For a Flash Sale, failing closed (returning false) is safer to prevent overselling.
                _logger.LogError(ex, "Error acquiring distributed lock for key: {LockKey}", lockKey);
                return false;
            }
        }

        public async Task ReleaseLockAsync(string lockKey, CancellationToken ct = default)
        {
            try
            {
                // LockReleaseAsync checks the value to ensure it only deletes the lock if it matches our lockValue.
                bool released = await _db.LockReleaseAsync(lockKey, _lockValue);
                if (released)
                {
                    _logger.LogDebug("Released distributed lock: {LockKey}", lockKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing distributed lock for key: {LockKey}", lockKey);
            }
        }
    }
}
