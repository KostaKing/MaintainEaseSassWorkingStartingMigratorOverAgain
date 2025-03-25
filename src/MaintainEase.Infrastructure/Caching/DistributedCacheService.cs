using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace MaintainEase.Infrastructure.Caching
{
    /// <summary>
    /// Implementation of cache service using distributed cache
    /// </summary>
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

        public DistributedCacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            var value = await factory();
            await SetAsync(key, value, expiration);
            
            return value;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }
            
            var cachedJson = System.Text.Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(cachedJson);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };
            
            var jsonValue = JsonSerializer.Serialize(value);
            var encodedValue = System.Text.Encoding.UTF8.GetBytes(jsonValue);
            
            await _distributedCache.SetAsync(key, encodedValue, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _distributedCache.RemoveAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var cachedValue = await _distributedCache.GetAsync(key);
            return cachedValue != null;
        }
    }
}
