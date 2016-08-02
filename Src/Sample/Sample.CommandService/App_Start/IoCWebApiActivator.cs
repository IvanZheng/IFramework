using System.Web.Http;
using IFramework.IoC.WebApi;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Sample.CommandService.App_Start.IoCWebApiActivator), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(typeof(Sample.CommandService.App_Start.IoCWebApiActivator), "Shutdown")]

namespace Sample.CommandService.App_Start
{
    /// <summary>Provides the bootstrapping for integrating Unity with WebApi when it is hosted in ASP.NET</summary>
    public static class IoCWebApiActivator
    {
        /// <summary>Integrates Unity when the application starts.</summary>
        public static void Start() 
        {
            var resolver = new HierarchicalDependencyResolver(IoCConfig.GetConfiguredContainer());
            //resolver.RegisterComponents(container =>
            //    Configuration.Instance
            //                 .RegisterEntityFrameworkComponents(container,
            //                                                    Lifetime.Hierarchical,
            //                                                    typeof(SampleModelContext),
            //                                                    typeof(CommunityRepository)
            //                                                    )
            //    );
            GlobalConfiguration.Configuration.DependencyResolver = resolver;
        }

        /// <summary>Disposes the Unity container when the application is shut down.</summary>
        public static void Shutdown()
        {
            var container = IoCConfig.GetConfiguredContainer();
            container.Dispose();
        }
    }
}
