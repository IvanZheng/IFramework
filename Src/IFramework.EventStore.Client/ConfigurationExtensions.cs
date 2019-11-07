using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using IFramework.DependencyInjection;
using IFramework.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFramework.EventStore.Client
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddEventStoreClient(this IServiceCollection services, Action<EventStoreOptions> options = null)
        {
            services.AddCustomOptions(options);
            services.AddSingleton(provider =>
            {
                var eventStoreOptions = provider.GetService<IOptions<EventStoreOptions>>().Value;
                return EventStoreConnection.Create(eventStoreOptions.ConnectionString,
                                                   eventStoreOptions.ConnectionName);
            }).AddSingleton<IEventStore, EventStore>();
            return services;
        }
    }
}
