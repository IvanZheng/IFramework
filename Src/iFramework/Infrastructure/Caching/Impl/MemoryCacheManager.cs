using System;
using Microsoft.Extensions.Caching.Memory;

namespace IFramework.Infrastructure.Caching.Impl
{
    /// <summary>
    ///     Represents a manager for caching between HTTP requests (long term caching)
    /// </summary>
    public class MemoryCacheManager : CacheManagerBase
    {
        protected MemoryCache Cache => new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        ///     Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key.</returns>
        public override CacheValue<T> Get<T>(string key)
        {
            return Cache.Get<CacheValue<T>>(key) ?? CacheValue<T>.NoValue;
        }

        /// <summary>
        ///     Adds the specified key and object to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="data">Data</param>
        /// <param name="cacheTime">Cache time</param>
        public override void Set<T>(string key, T data, int cacheTime)
        {
            Cache.Set(key,
                      new CacheValue<T>(data, true),
                      DateTime.Now + TimeSpan.FromMinutes(cacheTime));
        }

        /// <summary>
        ///     Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>Result</returns>
        public override bool IsSet(string key)
        {
            return Cache.TryGetValue(key, out var result);
        }

        /// <summary>
        ///     Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">/key</param>
        public override void Remove(string key)
        {
            Cache.Remove(key);
        }

        /// <summary>
        ///     Removes items by pattern
        /// </summary>
        /// <param name="pattern">pattern</param>
        public override void RemoveByPattern(string pattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Clear all cache data
        /// </summary>
        public override void Clear()
        {
            throw new NotImplementedException();
        }
    }
}