using System;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.EntityFrameworkCore.UnitOfWorks;
using IFramework.Infrastructure;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EntityFrameworkCore
{
    public static class ConfigurationExtension
    {
        public static Configuration UseDbContext<TDbContext>(this Configuration configuration, Action<DbContextOptionsBuilder> optionsAction)
            where TDbContext : DbContext
        {
            var services = new ServiceCollection();
            services.AddDbContext<TDbContext>(optionsAction);
            ObjectProviderFactory.Instance.Populate(services);
            return configuration;
        }
        public static Configuration UseDbContextPool<TDbContext>(this Configuration configuration, Action<DbContextOptionsBuilder> optionsAction, int poolSize = 128)
            where TDbContext : DbContext
        {
            var services = new ServiceCollection();
            services.AddDbContextPool<TDbContext>(optionsAction, poolSize);
            ObjectProviderFactory.Instance.Populate(services);
            return configuration;
        }

        /// <summary>
        ///     TDbContext is the default type for Repository<TEntity />'s dbContext injected paramter
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="builder"></param>
        /// <param name="defaultRepositoryType"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static Configuration UseEntityFrameworkComponents(this Configuration configuration,
                                                                 IObjectProviderBuilder builder,
                                                                 Type defaultRepositoryType,
                                                                 ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? ObjectProviderFactory.Instance.ObjectProviderBuilder;
            return configuration.RegisterUnitOfWork(builder, lifetime)
                                .RegisterRepositories(defaultRepositoryType, builder, lifetime);
        }

        public static Configuration UseEntityFrameworkComponents(this Configuration configuration,
                                                                 Type defaultRepositoryType,
                                                                 ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return configuration.UseEntityFrameworkComponents(null, defaultRepositoryType, lifetime);
        }

        public static Configuration RegisterUnitOfWork(this Configuration configuration,
                                                       IObjectProviderBuilder builder,
                                                       ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? ObjectProviderFactory.Instance.ObjectProviderBuilder;
            builder.Register<IUnitOfWork, UnitOfWorks.UnitOfWork>(lifetime);
            return configuration;
        }

        public static Configuration RegisterRepositories(this Configuration configuration,
                                                         Type repositoryType,
                                                         IObjectProviderBuilder builder,
                                                         ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? ObjectProviderFactory.Instance.ObjectProviderBuilder;
            builder.Register(typeof(IRepository<>), repositoryType, lifetime);
            builder.Register<IDomainRepository, DomainRepository>(lifetime);
            return configuration;
        }
    }
}