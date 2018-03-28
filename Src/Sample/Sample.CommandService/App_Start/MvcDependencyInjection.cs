using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Autofac.Integration.Mvc;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;

namespace Sample.CommandService.App_Start
{
    public static class DependencyInjectionExtension
    {
        public static HttpConfiguration RegisterMvcDependencyInjection(this HttpConfiguration configuration)
        {
            var objectProvider = (ObjectProvider)ObjectProviderFactory.Instance.ObjectProvider;

            FilterProviders.Providers.Remove(FilterProviders.Providers.OfType<FilterAttributeFilterProvider>().First());
            FilterProviders.Providers.Add(new AutofacFilterProvider());
            DependencyResolver.SetResolver(new AutofacDependencyResolver(objectProvider.Scope));
            return configuration;
        }

        public static HttpConfiguration RegisterWebApiDependencyInjection(this HttpConfiguration configuration)
        {
            var resolver = new HierarchicalDependencyResolver(ObjectProviderFactory.Instance.ObjectProvider);
            configuration.DependencyResolver = resolver;
            return configuration;
        }
    }
}