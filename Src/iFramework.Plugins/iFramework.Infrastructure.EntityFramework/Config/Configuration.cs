using IFramework.Config;
using IFramework.EntityFramework.Repositories;
using IFramework.IoC;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using DomainRepository = IFramework.EntityFramework.Repositories.DomainRepository;

namespace IFramework.EntityFramework.Config
{
    public static class ConfigurationExtension
    {
        public static Configuration RegisterEntityFrameworkComponents(this Configuration configuration,
                                                                      IContainer container,
                                                                      Lifetime lifetime = Lifetime.Hierarchical)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            return configuration.RegisterUnitOfWork(container, lifetime)
                                .RegisterRepositories(container, lifetime);
        }

        public static Configuration RegisterEntityFrameworkComponents(this Configuration configuration,
                                                                      Lifetime lifetime = Lifetime.Hierarchical)
        {
            return configuration.RegisterEntityFrameworkComponents(null, lifetime);
        }

        //public static Configuration RegisterDbContext(this Configuration configuration, IContainer container, Lifetime lifetime = Lifetime.PerRequest, params Type[] dbContextTypes)
        //{
        //    container = container ?? IoCFactory.Instance.CurrentContainer;
        //    dbContextTypes.ForEach(type =>
        //    {
        //        container.RegisterType(type, type, lifetime);
        //    });
        //    return configuration;
        //}

        public static Configuration RegisterUnitOfWork(this Configuration configuration,
                                                       IContainer container,
                                                       Lifetime lifetime = Lifetime.Hierarchical)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            container.RegisterType<IUnitOfWork, UnitOfWork>(lifetime);
            container.RegisterType<IAppUnitOfWork, AppUnitOfWork>(lifetime);
            return configuration;
        }

        public static Configuration RegisterRepositories(this Configuration configuration,
                                                         IContainer container,
                                                         Lifetime lifetime = Lifetime.Hierarchical)
        {
            container = container ?? IoCFactory.Instance.CurrentContainer;
            container.RegisterType(typeof(IRepository<>), typeof(Repository<>));
            container.RegisterType<IDomainRepository, DomainRepository>(lifetime);
            return configuration;
        }
    }
}