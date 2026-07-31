using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Enums;
using FlashSale.Application.Common.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace FlashSale.Infrastructure.Caching
{
    public class RedisFlashSaleStockCache : IFlashSaleStockCache
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisFlashSaleStockCache> _logger;
        private readonly IDatabase _db;

        private const string KeyPrefix = "flashsale:";
        private const string KeySuffix = ":stock";

        public RedisFlashSaleStockCache(IConnectionMultiplexer redis, ILogger<RedisFlashSaleStockCache> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
        }

        private string GetKey(Guid flashSaleItemId) => $"{KeyPrefix}{flashSaleItemId}{KeySuffix}";

        public async Task<bool> InitStockAsync(Guid flashSaleItemId, int saleStock, CancellationToken ct = default)
        {
            var key = GetKey(flashSaleItemId);
            // Không set TTL, key sống suốt vòng đời FlashSale Active
            return await _db.StringSetAsync(key, saleStock);
        }

        public async Task<StockDecrementResult> TryDecrementStockAsync(Guid flashSaleItemId, int quantity, CancellationToken ct = default)
        {
            var key = GetKey(flashSaleItemId);
            var result = (int)await _db.ScriptEvaluateAsync(
                FlashSaleLuaScripts.DecrementStockScript,
                new RedisKey[] { key },
                new RedisValue[] { quantity }
            );

            return result switch
            {
                -1 => StockDecrementResult.StockNotInitialized,
                -2 => StockDecrementResult.InsufficientStock,
                _ => StockDecrementResult.Success
            };
        }

        public async Task<int> IncrementStockAsync(Guid flashSaleItemId, int quantity, CancellationToken ct = default)
        {
            var key = GetKey(flashSaleItemId);
            var result = (int)await _db.ScriptEvaluateAsync(
                FlashSaleLuaScripts.IncrementStockScript,
                new RedisKey[] { key },
                new RedisValue[] { quantity }
            );

            return result;
        }

        public async Task<int?> GetCurrentStockAsync(Guid flashSaleItemId, CancellationToken ct = default)
        {
            var key = GetKey(flashSaleItemId);
            var stock = await _db.StringGetAsync(key);
            
            if (stock.IsNullOrEmpty)
                return null;
                
            return (int)stock;
        }
    }
}
