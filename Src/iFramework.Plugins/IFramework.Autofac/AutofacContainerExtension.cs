using Autofac.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Autofac
{
    internal static class AutofacContainerExtension
    {
        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> 
            InstanceLifetime<TLimit, TActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder, IoC.Lifetime lifetime)
        {
            switch(lifetime)
            {

            }
            //InstancePerRequest()
            //    .InstancePerDependency()
            //    .InstancePerLifetimeScope()
            //    .InstancePerMatchingLifetimeScope()
            //    .InstancePerOwned()
            //    .SingleInstance
            return builder;
        }
    }
}
