using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    internal static class AutofacExtension
    {
        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
            InstanceLifetime<TLimit, TActivatorData, TRegistrationStyle>(
                this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder,
                ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    builder.SingleInstance();
                    break;
                case ServiceLifetime.Scoped:
                    builder.InstancePerLifetimeScope();
                    break;
                case ServiceLifetime.Transient:
                    builder.InstancePerDependency();
                    break;
            }
            return builder;
        }
    }
}
