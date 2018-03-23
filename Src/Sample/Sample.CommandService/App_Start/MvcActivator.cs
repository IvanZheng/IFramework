using System.Linq;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Sample.CommandService.App_Start.AutofacMvcActivator), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethod(typeof(Sample.CommandService.App_Start.AutofacMvcActivator), "Shutdown")]

namespace Sample.CommandService.App_Start
{
    /// <summary>Provides the bootstrapping for integrating Unity with ASP.NET MVC.</summary>
    //public static class UnityMvcActivator
    //{
    //    /// <summary>Integrates Unity when the application starts.</summary>
    //    static IContainer _container;
    //    public static void Start()
    //    {
    //        _container = IoCConfig.GetMvcConfiguredContainer();


    //        FilterProviders.Providers.Remove(FilterProviders.Providers.OfType<FilterAttributeFilterProvider>().First());
    //        FilterProviders.Providers.Add(new UnityFilterAttributeFilterProvider(_container.GetUnityContainer()));

    //        DependencyResolver.SetResolver(new IFramework.Unity.Mvc.UnityDependencyResolver(_container));

    //        // TODO: Uncomment if you want to use PerRequestLifetimeManager
    //        Microsoft.Web.Infrastructure.DynamicModuleHelper.DynamicModuleUtility.RegisterModule(typeof(UnityPerRequestHttpModule));
    //    }

    //    /// <summary>Disposes the Unity container when the application is shut down.</summary>
    //    public static void Shutdown()
    //    {
    //        _container?.Dispose();
    //    }
    //}


    public static class AutofacMvcActivator
    {
        static ObjectProvider _container;

        /// <summary>Integrates Autofac when the application starts.</summary>
        public static void Start()
        {
            _container = (ObjectProvider)IoCFactory.Instance.ObjectProvider;

            FilterProviders.Providers.Remove(FilterProviders.Providers.OfType<FilterAttributeFilterProvider>().First());
            FilterProviders.Providers.Add(new AutofacFilterProvider());
            DependencyResolver.SetResolver(new AutofacDependencyResolver(_container.Scope));
        }

        /// <summary>Disposes the Autofac container when the application is shut down.</summary>
        public static void Shutdown()
        {
            _container?.Dispose();
        }
    }
}