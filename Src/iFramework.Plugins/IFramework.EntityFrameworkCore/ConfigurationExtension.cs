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
        /// <summary>
        ///     TDbContext is the default type for Repository<TEntity />'s dbContext injected paramter
        /// </summary>
        /// <param name="services"></param>
        /// <param name="defaultRepositoryType"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkComponents(this IServiceCollection services,
                                                                      Type defaultRepositoryType,
                                                                      ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddUnitOfWork(lifetime)
                                .AddRepositories(defaultRepositoryType,  lifetime);
        }

        public static IServiceCollection AddUnitOfWork(this IServiceCollection services,
                                                       ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.AddService<IUnitOfWork, UnitOfWorks.UnitOfWork>(lifetime);
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services,
                                                         Type repositoryType,
                                                         ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.AddService(typeof(IRepository<>), repositoryType, lifetime);
            services.AddService<IDomainRepository, DomainRepository>(lifetime);
            return services;
        }
    }
}