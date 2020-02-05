using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Infrastructure.EventSourcing.Stores;

namespace IFramework.Infrastructure.EventSourcing.Configurations
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddEventSourcingComponents(this IServiceCollection services)
        {
            services.AddScoped(typeof(IEventSourcingRepository<>), typeof(EventSourcingRepository<>))
                    .AddScoped<IEventSourcingUnitOfWork, UnitOfWork>()
                    .AddSingleton<IInMemoryStore, InMemoryStore>();
            return services;
        }
    }
}
