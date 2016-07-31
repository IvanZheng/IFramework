using Autofac;
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
                case IoC.Lifetime.PerRequest:
                    builder.InstancePerRequest();
                    break;
                case IoC.Lifetime.Singleton:
                    builder.SingleInstance();
                    break;
                case IoC.Lifetime.Hierarchical:
                    builder.InstancePerLifetimeScope();
                    break;
            }
            return builder;
        }
    }
}
