using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Caching
{
    public static class CacheExtensions
    {
        /// <summary>
        ///     Default cache timeout is 60 minutes.
        /// </summary>
        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="acquire">Function to load item if it's not in the cache yet</param>
        /// <returns>Cached item</returns>
        public static CacheValue<T> Get<T>(this ICacheManager cacheManager, string key, Func<T> acquire)
        {
            return Get(cacheManager, key, 60, acquire);
        }

        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="cacheTime">Cache time in minutes (0 - do not cache)</param>
        /// <param name="acquire">Function to load item if it's not in the cache yet</param>
        /// <returns>Cached item</returns>
        public static CacheValue<T> Get<T>(this ICacheManager cacheManager, string key, int cacheTime, Func<T> acquire)
        {
            if (cacheTime <= 0)
            {
                return new CacheValue<T>(acquire(), true);
            }

            var cacheValue = cacheManager.Get<T>(key);
            if (!cacheValue.HasValue)
            {
                cacheValue = new CacheValue<T>(acquire(), true);
                cacheManager.Set(key, cacheValue.Value, cacheTime);
            }
            return cacheValue;
        }

        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        ///     Default cache timeout is 60 minutes.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="acquire">async Function to load item if it's not in the cache yet</param>
        /// <param name="continueOnCapturedContext"></param>
        /// <returns>Cached item</returns>
        public static Task<CacheValue<T>> GetAsync<T>(this ICacheManager cacheManager,
                                                      string key,
                                                      Func<Task<T>> acquire,
                                                      bool continueOnCapturedContext = false)
        {
            return cacheManager.GetAsync(key, 60, acquire, continueOnCapturedContext);
        }


        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="cacheTime">Cache time in minutes (0 - do not cache)</param>
        /// <param name="acquire">Async Function to load item if it's not in the cache yet</param>
        /// <param name="continueOnCapturedContext"></param>
        /// <returns>Cached item</returns>
        public static async Task<CacheValue<T>> GetAsync<T>(this ICacheManager cacheManager,
                                                            string key,
                                                            int cacheTime,
                                                            Func<Task<T>> acquire,
                                                            bool continueOnCapturedContext = false)
        {
            if (cacheTime <= 0)
            {
                return new CacheValue<T>(await acquire().ConfigureAwait(continueOnCapturedContext), true);
            }

            var cacheValue = await cacheManager.GetAsync<T>(key)
                                               .ConfigureAwait(continueOnCapturedContext);
            if (!cacheValue.HasValue)
            {
                cacheValue = new CacheValue<T>(await acquire().ConfigureAwait(continueOnCapturedContext), true);
                await cacheManager.SetAsync(key, cacheValue.Value, cacheTime)
                                  .ConfigureAwait(continueOnCapturedContext);
                return cacheValue;
            }
            return cacheValue;
        }


        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        ///     Default cache timeout is 60 minutes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheManager"></param>
        /// <param name="key"></param>
        /// <param name="acquire">sync function</param>
        /// <param name="continueOnCapturedContext"></param>
        /// <returns></returns>
        public static Task<CacheValue<T>> GetAsync<T>(this ICacheManager cacheManager,
                                                      string key,
                                                      Func<T> acquire,
                                                      bool continueOnCapturedContext = false)
        {
            return cacheManager.GetAsync(key, 60, acquire, continueOnCapturedContext);
        }

        /// <summary>
        ///     Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheManager"></param>
        /// <param name="key"></param>
        /// <param name="cacheTime">cache in minutes</param>
        /// <param name="acquire">sync function</param>
        /// <param name="continueOnCapturedContext"></param>
        /// <returns></returns>
        public static async Task<CacheValue<T>> GetAsync<T>(this ICacheManager cacheManager,
                                                            string key,
                                                            int cacheTime,
                                                            Func<T> acquire,
                                                            bool continueOnCapturedContext = false)
        {
            if (cacheTime <= 0)
            {
                return new CacheValue<T>(acquire(), true);
            }

            var cacheValue = await cacheManager.GetAsync<T>(key)
                                               .ConfigureAwait(continueOnCapturedContext);
            if (!cacheValue.HasValue)
            {
                cacheValue = new CacheValue<T>(acquire(), true);
                await cacheManager.SetAsync(key, cacheValue.Value, cacheTime)
                                  .ConfigureAwait(continueOnCapturedContext);

                return cacheValue;
            }
            return cacheValue;
        }
    }
}