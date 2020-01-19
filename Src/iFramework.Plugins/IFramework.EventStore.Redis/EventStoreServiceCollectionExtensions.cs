using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Infrastructure.EventSourcing.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EventStore.Redis
{
    public static class EventStoreServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisEventStore(this IServiceCollection services, Action<RedisEventStoreOptions> options = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddCustomOptions(options);
            services.AddSingleton<IEventStore, EventStore>();
            return services;
        }

        public static IServiceCollection AddRedisSnapshotStore(this IServiceCollection services, Action<RedisSnapshotStoreOptions> options = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddCustomOptions(options);
            services.AddSingleton<ISnapshotStore, RedisSnapshotStore>();
            return services;
        }
    }
}
