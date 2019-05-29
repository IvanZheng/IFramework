using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Message.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public static class ObjectProviderBuilderExtension
    {
        private static readonly ConcurrentBag<Type> RegisteredHandlerTypes = new ConcurrentBag<Type>();
        /// <summary>
        ///     Register MessageHandlers with AOP features
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="handlerProviderNames"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IObjectProviderBuilder RegisterMessageHandlers(this IObjectProviderBuilder builder,
                                                                     string[] handlerProviderNames,
                                                                     ServiceLifetime lifetime)
        {
            var handlers = Configuration.Instance
                                        .GetSection(HandlerProvider.FrameworkConfigurationSectionName)
                                        ?.Get<FrameworkConfiguration>()
                                        ?.Handlers;
            if (handlers != null)
            {
                foreach (var handlerElement in handlers)
                {
                    if (handlerProviderNames == null || handlerProviderNames.Contains(handlerElement.Name))
                    {
                        switch (handlerElement.SourceType)
                        {
                            case HandlerSourceType.Type:
                                var type = Type.GetType(handlerElement.Source);
                                RegisterHandlerFromType(builder, type, lifetime);
                                break;
                            case HandlerSourceType.Assembly:
                                var assembly = Assembly.Load(handlerElement.Source);
                                RegisterHandlerFromAssembly(builder, assembly, lifetime);
                                break;
                        }
                    }
                }
            }
            return builder;
        }

        private static void RegisterHandlerFromAssembly(IObjectProviderBuilder builder, Assembly assembly, ServiceLifetime lifetime)
        {
            var handlerGenericTypes = new[]
                                      {
                                          typeof(ICommandAsyncHandler<ICommand>),
                                          typeof(ICommandHandler<ICommand>),
                                          typeof(IEventSubscriber<IEvent>),
                                          typeof(IEventAsyncSubscriber<IEvent>)
                                      }
                                      .Select(ht => ht.GetGenericTypeDefinition())
                                      .ToArray();

            var exportedTypes = assembly.GetExportedTypes()
                                        .Where(x => !x.IsInterface && !x.IsAbstract
                                                                           && x.GetInterfaces()
                                                                               .Any(y => y.IsGenericType
                                                                                         && handlerGenericTypes.Contains(y.GetGenericTypeDefinition())));
            foreach (var type in exportedTypes)
            {
                RegisterHandlerFromType(builder, type, lifetime);
            }
        }

        private static void RegisterHandlerFromType(IObjectProviderBuilder builder, Type type, ServiceLifetime lifetime)
        {
            if (!RegisteredHandlerTypes.Contains(type))
            {
                builder.Register(type, 
                                 type, 
                                 lifetime, 
                                 new VirtualMethodInterceptorInjection(),
                                 new InterceptionBehaviorInjection());
                RegisteredHandlerTypes.Add(type);
            }
        }
    }
}