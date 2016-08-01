using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.EntityFramework.Config
{
    public static class ConfigurationExtension
    {
        public static Configuration RegisterEntityFrameworkComponents(this Configuration configuration, IContainer container, Lifetime lifetime = Lifetime.PerRequest, params Type[] dbContextTypes)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            return configuration.RegisterUnitOfWork(container, lifetime)
                                .RegisterRepositories(container, lifetime)
                                .RegisterDbContext(container, lifetime, dbContextTypes);
        }

        public static Configuration RegisterEntityFrameworkComponents(this Configuration configuration, Lifetime lifetime = Lifetime.PerRequest, params Type[] dbContextTypes)
        {
            return configuration.RegisterEntityFrameworkComponents(null, lifetime, dbContextTypes);
        }

        public static Configuration RegisterDbContext(this Configuration configuration, IContainer container, Lifetime lifetime = Lifetime.PerRequest, params Type[] dbContextTypes)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            dbContextTypes.ForEach(type =>
            {
                container.RegisterType(type, type, lifetime);
            });
            return configuration;
        }

        public static Configuration RegisterUnitOfWork(this Configuration configuration, IContainer container, Lifetime lifetime = Lifetime.PerRequest)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            container.RegisterType<IUnitOfWork, EntityFramework.UnitOfWork>(lifetime);
            return configuration;
        }
        public static Configuration RegisterRepositories(this Configuration configuration, IContainer container, Lifetime lifetime = Lifetime.PerRequest)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            container.RegisterType(typeof(IRepository<>), typeof(EntityFramework.Repositories.Repository<>), lifetime);
            container.RegisterType<IDomainRepository, EntityFramework.Repositories.DomainRepository>(lifetime);
            return configuration;
        }
    }
}
