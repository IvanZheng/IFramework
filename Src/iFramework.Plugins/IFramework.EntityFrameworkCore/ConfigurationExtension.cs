using IFramework.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.EntityFrameworkCore.Repositories;
using IFramework.EntityFrameworkCore.UnitOfWorks;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EntityFrameworkCore
{
    public static class ConfigurationExtension
    {
        public static Configuration UseEntityFrameworkComponents(this Configuration configuration,
                                                                      IObjectProviderBuilder builder,
                                                                      ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? IoCFactory.Instance.ObjectProviderBuilder;
            return configuration.RegisterUnitOfWork(builder, lifetime)
                                .RegisterRepositories(builder, lifetime);
        }

        public static Configuration UseEntityFrameworkComponents(this Configuration configuration,
                                                                      ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return configuration.UseEntityFrameworkComponents(null, lifetime);
        }

        public static Configuration RegisterUnitOfWork(this Configuration configuration,
                                                       IObjectProviderBuilder builder,
                                                       ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? IoCFactory.Instance.ObjectProviderBuilder;
            builder.Register<IUnitOfWork, UnitOfWorks.UnitOfWork>(lifetime);
            builder.Register<IAppUnitOfWork, AppUnitOfWork>(lifetime);
            return configuration;
        }

        public static Configuration RegisterRepositories(this Configuration configuration,
                                                         IObjectProviderBuilder builder,
                                                         ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? IoCFactory.Instance.ObjectProviderBuilder;
            builder.Register(typeof(IRepository<>), typeof(Repository<>));
            builder.Register<IDomainRepository, DomainRepository>(lifetime);
            return configuration;
        }
    }
}
