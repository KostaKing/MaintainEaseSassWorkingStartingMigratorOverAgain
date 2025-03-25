using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MaintainEase.Infrastructure.Caching;

namespace MaintainEase.Infrastructure.Caching.Fusion
{
    /// <summary>
    /// Fusion cache service that combines memory and distributed cache
    /// </summary>
    public class FusionCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<FusionCacheService> _logger;
        private readonly TimeSpan _defaultMemoryExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _defaultDistributedExpiration = TimeSpan.FromMinutes(30);

        public FusionCacheService(
            IMemoryCache memoryCache, 
            IDistributedCache distributedCache,
            ILogger<FusionCacheService> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get or set a cached value with a factory method
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Try to get from memory cache first
            if (_memoryCache.TryGetValue(key, out T memoryValue))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return memoryValue;
            }

            // Try to get from distributed cache
            try
            {
                var distributedValue = await GetFromDistributedCacheAsync<T>(key);
                if (distributedValue != null)
                {
                    _logger.LogDebug("Cache hit (distributed): {Key}", key);
                    
                    // Store in memory cache for faster access next time
                    StoreInMemoryCache(key, distributedValue, expiration);
                    
                    return distributedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from distributed cache: {Key}", key);
                // Continue to factory method - don't let cache issues prevent functionality
            }

            // If not found in any cache, use factory
            _logger.LogDebug("Cache miss: {Key}", key);
            var value = await factory();

            // Store in both caches
            try
            {
                StoreInMemoryCache(key, value, expiration);
                await StoreInDistributedCacheAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error storing in cache: {Key}", key);
                // Don't let cache issues prevent returning the value
            }

            return value;
        }

        /// <summary>
        /// Get a value from cache
        /// </summary>
        public async Task<T> GetAsync<T>(string key)
        {
            // Try memory cache first
            if (_memoryCache.TryGetValue(key, out T memoryValue))
            {
                return memoryValue;
            }

            // Try distributed cache
            try
            {
                var distributedValue = await GetFromDistributedCacheAsync<T>(key);
                if (distributedValue != null)
                {
                    // Store in memory cache for faster access next time
                    StoreInMemoryCache(key, distributedValue, null);
                }
                return distributedValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from distributed cache: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Set a value in cache
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                StoreInMemoryCache(key, value, expiration);
                await StoreInDistributedCacheAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error storing in cache: {Key}", key);
            }
        }

        /// <summary>
        /// Remove a value from cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error removing from cache: {Key}", key);
            }
        }

        /// <summary>
        /// Check if a key exists in cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            // Check distributed cache
            try
            {
                var cachedValue = await _distributedCache.GetAsync(key);
                return cachedValue != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking existence in distributed cache: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Get value from distributed cache
        /// </summary>
        private async Task<T> GetFromDistributedCacheAsync<T>(string key)
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }
            
            var cachedJson = System.Text.Encoding.UTF8.GetString(cachedBytes);
            return System.Text.Json.JsonSerializer.Deserialize<T>(cachedJson);
        }

        /// <summary>
        /// Store value in memory cache
        /// </summary>
        private void StoreInMemoryCache<T>(string key, T value, TimeSpan? expiration)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultMemoryExpiration,
                // Add sliding expiration to keep frequently-used items
                SlidingExpiration = TimeSpan.FromMinutes(1)
            };
            
            _memoryCache.Set(key, value, options);
        }

        /// <summary>
        /// Store value in distributed cache
        /// </summary>
        private async Task StoreInDistributedCacheAsync<T>(string key, T value, TimeSpan? expiration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultDistributedExpiration
            };
            
            var jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
            var encodedValue = System.Text.Encoding.UTF8.GetBytes(jsonValue);
            
            await _distributedCache.SetAsync(key, encodedValue, options);
        }
    }
}
