using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundatio.Caching;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;

namespace IFramework.FoundatioRedis.Caching
{
    public class RedisCacheManager : ICacheManager
    {
        protected RedisCacheClient CacheClient;

        public RedisCacheManager(RedisCacheClient cacheClient)
        {
            CacheClient = cacheClient;
        }

        public virtual Infrastructure.Caching.CacheValue<T> Get<T>(string key)
        {
            return GetAsync<T>(key).Result;
        }

        public virtual async Task<Infrastructure.Caching.CacheValue<T>> GetAsync<T>(string key)
        {
            var cacheValue = await CacheClient.GetAsync<T>(key);
            return cacheValue.HasValue ?
                new Infrastructure.Caching.CacheValue<T>(cacheValue.Value, true) :
                Infrastructure.Caching.CacheValue<T>.NoValue;
        }

        public virtual void Set(string key, object data, int cacheTime)
        {
            SetAsync(key, data, cacheTime).Wait();
        }

        public virtual Task SetAsync(string key, object data, int cacheTime)
        {
            return CacheClient.SetAsync(key, data, new TimeSpan(0, 0, cacheTime, 0));
        }

        public virtual bool IsSet(string key)
        {
            return IsSetAsync(key).Result;
        }

        public virtual Task<bool> IsSetAsync(string key)
        {
            return CacheClient.ExistsAsync(key);
        }

        public virtual void Remove(string key)
        {
            CacheClient.RemoveAsync(key).Wait();
        }

        public virtual Task RemoveAsync(string key)
        {
            return CacheClient.RemoveAsync(key);
        }

        public virtual void RemoveByPattern(string pattern)
        {
            CacheClient.RemoveByPrefixAsync(pattern).Wait();
        }

        public virtual Task RemoveByPatternAsync(string pattern)
        {
            return CacheClient.RemoveByPrefixAsync(pattern);
        }

        public virtual void Clear()
        {
            CacheClient.RemoveAllAsync().Wait();
        }

        public virtual Task ClearAsync()
        {
            return CacheClient.RemoveAllAsync();
        }
    }
}
