using Autofac;
using Autofac.Builder;
using IFramework.IoC;
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
            InstanceLifetime<TLimit, TActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder, Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.PerRequest:
                    builder.InstancePerRequest();
                    break;
                case Lifetime.Singleton:
                    builder.SingleInstance();
                    break;
                case Lifetime.Hierarchical:
                    builder.InstancePerLifetimeScope();
                    break;
                case Lifetime.Transient:
                    builder.InstancePerDependency();
                    break;
            }
            return builder;
        }
    }
}
