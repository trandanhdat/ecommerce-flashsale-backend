using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.DTOs;
using FlashSale.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.Caching
{
    public class RedisProductCatalogCache : IProductCatalogCache
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisProductCatalogCache> _logger;
        private readonly IDatabase _db;

        // Định dạng tên Key theo yêu cầu
        private const string ProductKeyPrefix = "product:";
        private const string ProductListKeyPrefix = "product:list:";
        private const string ProductListKeysSet = "product:list:keys";

        // Mặc định TTL theo yêu cầu
        private readonly TimeSpan DefaultProductTtl = TimeSpan.FromMinutes(5);
        private readonly TimeSpan DefaultListTtl = TimeSpan.FromMinutes(2);

        public RedisProductCatalogCache(IConnectionMultiplexer redis, ILogger<RedisProductCatalogCache> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
        }

        public async Task<ProductCatalogCacheDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
        {
            try
            {
                string key = $"{ProductKeyPrefix}{productId}";
                var value = await _db.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<ProductCatalogCacheDto>(value!);
            }
            // Fail-open: Bắt các lỗi kết nối Redis, trả về null (coi như Cache Miss) thay vì làm sập DB chính
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi GetProductAsync cho ProductId {ProductId}", productId);
                return null;
            }
        }

        public async Task SetProductAsync(ProductCatalogCacheDto dto, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            try
            {
                string key = $"{ProductKeyPrefix}{dto.Id}";
                var json = JsonSerializer.Serialize(dto);
                await _db.StringSetAsync(key, json, ttl ?? DefaultProductTtl);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi SetProductAsync cho ProductId {ProductId}", dto.Id);
            }
        }

        public async Task RemoveProductAsync(Guid productId, CancellationToken ct = default)
        {
            try
            {
                string key = $"{ProductKeyPrefix}{productId}";
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi RemoveProductAsync cho ProductId {ProductId}", productId);
            }
        }

        public async Task<IEnumerable<ProductCatalogCacheDto>?> GetProductListAsync(int page, int pageSize, Guid? categoryId, CancellationToken ct = default)
        {
            try
            {
                string catKey = categoryId?.ToString() ?? "all";
                string key = $"{ProductListKeyPrefix}{catKey}:{page}:{pageSize}";

                var value = await _db.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<List<ProductCatalogCacheDto>>(value!);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi GetProductListAsync");
                return null;
            }
        }

        public async Task SetProductListAsync(int page, int pageSize, Guid? categoryId, IEnumerable<ProductCatalogCacheDto> items, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            try
            {
                string catKey = categoryId?.ToString() ?? "all";
                string key = $"{ProductListKeyPrefix}{catKey}:{page}:{pageSize}";

                var json = JsonSerializer.Serialize(items);
                
                // Set dữ liệu vào cache
                await _db.StringSetAsync(key, json, ttl ?? DefaultListTtl);

                // Add key này vào Set để phục vụ việc Invalidate hàng loạt
                await _db.SetAddAsync(ProductListKeysSet, key);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi SetProductListAsync");
            }
        }

        public async Task InvalidateProductListCacheAsync(CancellationToken ct = default)
        {
            try
            {
                // Lấy toàn bộ keys danh sách từ Set
                var listKeys = await _db.SetMembersAsync(ProductListKeysSet);
                if (listKeys != null && listKeys.Length > 0)
                {
                    // Convert thành mảng RedisKey để xóa hàng loạt
                    var keysToDelete = listKeys.Select(k => (RedisKey)k.ToString()).ToArray();
                    await _db.KeyDeleteAsync(keysToDelete);
                }

                // Cuối cùng xóa luôn cái Set đi để làm sạch
                await _db.KeyDeleteAsync(ProductListKeysSet);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
            {
                _logger.LogWarning(ex, "Lỗi kết nối Redis khi InvalidateProductListCacheAsync");
            }
        }
    }
}
