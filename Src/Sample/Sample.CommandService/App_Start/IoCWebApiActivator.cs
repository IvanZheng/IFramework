using System.Web.Http;
using IFramework.IoC.WebApi;
using Sample.CommandService.App_Start;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(IoCWebApiActivator), "Start")]
[assembly: ApplicationShutdownMethod(typeof(IoCWebApiActivator), "Shutdown")]

namespace Sample.CommandService.App_Start
{
    /// <summary>Provides the bootstrapping for integrating Unity with WebApi when it is hosted in ASP.NET</summary>
    public static class IoCWebApiActivator
    {
        /// <summary>Integrates Unity when the application starts.</summary>
        public static void Start()
        {
            var resolver = new HierarchicalDependencyResolver(IoCConfig.GetConfiguredContainer());
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