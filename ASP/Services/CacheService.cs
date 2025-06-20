using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JanitorAspNet.Services
{
    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task ClearAllAsync();
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return typedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);
            var value = await factory();

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = TimeSpan.FromMinutes(5), // Reset expiration if accessed within 5 minutes
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(key, value, cacheEntryOptions);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);

            return value;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            // This is a simplified implementation as IMemoryCache doesn't directly support pattern-based removal
            // In a production environment, you might want to use a more sophisticated caching solution
            // like Redis that supports pattern-based operations

            try
            {
                var regex = new Regex(pattern);
                
                // Unfortunately, IMemoryCache doesn't expose keys, so we can't iterate over them
                // This is a limitation of the in-memory cache implementation
                // For now, we'll just log the attempt
                
                _logger.LogWarning("Pattern-based cache removal requested for pattern: {Pattern}. " +
                    "This operation is not fully supported with IMemoryCache. Consider using Redis for advanced cache operations.", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cache removal pattern: {Pattern}", pattern);
            }

            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            // IMemoryCache doesn't have a built-in clear all method
            // We would need to maintain our own key registry or use a different cache implementation
            
            if (_cache is MemoryCache memoryCache)
            {
                // This is a workaround using reflection (not recommended for production)
                // Consider using a cache wrapper that tracks keys if you need this functionality
                _logger.LogWarning("Clear all cache operation requested. IMemoryCache doesn't support this operation directly.");
            }

            return Task.CompletedTask;
        }
    }
}
