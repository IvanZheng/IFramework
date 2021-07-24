using System;
using ECommon.Configurations;
using EQueue.Clients.Producers;
using EQueue.Configurations;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.EQueue;
using Microsoft.Extensions.DependencyInjection;
using ECommonConfiguration = ECommon.Configurations.Configuration;

namespace IFramework.Config
{
    public static class ConfigurationEQueue
    {
        private static void InitializeEqueue()
        {
            ECommonConfiguration.Create()
                                .UseAutofac()
                                .RegisterCommonComponents()
                                .UseLog4Net()
                                .UseJsonNet()
                                .RegisterUnhandledExceptionHandler()
                                .RegisterEQueueComponents()
                                .UseDeleteMessageByCountStrategy(10)
                                .SetDefault<IQueueSelector, QueueAverageSelector>()
                                .BuildContainer();
        }

        public static IServiceCollection AddEQueue(this IServiceCollection services,
                                              string nameServerAddresses = null,
                                              string clusterName = "DefaultCluster",
                                              int nameServerPort = 9493,
                                              Action<MessageQueueOptions> mqOptions = null)
        {
            if (mqOptions == null)
            {
                services.AddCustomOptions<MessageQueueOptions>(o =>
                {
                    o.EnableIdempotent = true;
                    o.EnsureArrival = true;
                    o.PersistEvent = true;
                });
            }
            else
            {
                services.AddCustomOptions(mqOptions);
            }
            InitializeEqueue();

            services.AddSingleton<IMessageQueueClientProvider, EQueueClientProvider>(new ConstructInjection(new ParameterInjection("clusterName", clusterName),
                                                                                               new ParameterInjection("nameServerList", nameServerAddresses),
                                                                                               new ParameterInjection("nameServerPort", nameServerPort)))
                      .AddSingleton<IMessageQueueClient, MessageQueueClient>();

            return services;
        }
    }
}