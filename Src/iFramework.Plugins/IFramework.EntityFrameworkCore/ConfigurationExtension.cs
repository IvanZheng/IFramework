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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EntityFrameworkCore
{
    public static class ConfigurationExtension
    {
        public static Configuration UseDbContextPool<TDbContext>(this Configuration configuration, Action<DbContextOptionsBuilder> optionsAction, int poolSize = 128)
            where TDbContext : DbContext
        {
            var services = new ServiceCollection();
            services.AddDbContextPool<TDbContext>(optionsAction, poolSize);
            IoCFactory.Instance.Populate(services);
            return configuration;
        }

        /// <summary>
        /// TDbContext is the default type for Repository<TEntity>'s dbContext injected paramter
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="builder"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static Configuration UseEntityFrameworkComponents<TDbContext>(this Configuration configuration,
                                                                      IObjectProviderBuilder builder,
                                                                      ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TDbContext : MsDbContext
        {
            builder = builder ?? IoCFactory.Instance.ObjectProviderBuilder;
            builder.Register<TDbContext, TDbContext>(lifetime);
            builder.Register<MsDbContext>(provider => provider.GetService<TDbContext>(), lifetime);
            return configuration.RegisterUnitOfWork(builder, lifetime)
                                .RegisterRepositories(builder, lifetime);
        }

        public static Configuration UseEntityFrameworkComponents<TDbContext>(this Configuration configuration,
                                                                             ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TDbContext : MsDbContext
        {
            return configuration.UseEntityFrameworkComponents<TDbContext>(null, lifetime);
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
