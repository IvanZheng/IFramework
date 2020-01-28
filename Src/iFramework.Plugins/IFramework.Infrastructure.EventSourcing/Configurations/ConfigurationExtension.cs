using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.Infrastructure.EventSourcing.Repositories;

namespace IFramework.Infrastructure.EventSourcing.Configurations
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddEventSourcing(this IServiceCollection services)
        {
            services.RegisterType(typeof(IEventSourcingRepository<>), typeof(EventSourcingRepository<>), ServiceLifetime.Scoped);
            return services;
        }
    }
}
