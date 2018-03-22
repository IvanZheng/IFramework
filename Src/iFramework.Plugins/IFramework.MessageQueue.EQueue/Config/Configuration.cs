using ECommon.Configurations;
using EQueue.Clients.Producers;
using EQueue.Configurations;
using IFramework.DependencyInjection;
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

        public static Configuration UseEQueue(this Configuration configuration,
                                              string nameServerAddresses = null,
                                              string clusterName = "DefaultCluster",
                                              int defaultPort = 9493)
        {
            InitializeEqueue();

            IoCFactory.Instance
                      .ObjectProviderBuilder
                      .Register<IMessageQueueClientProvider, EQueueClientProvider>(ServiceLifetime.Singleton,
                                                                                   new ConstructInjection(new ParameterInjection("clusterName", clusterName),
                                                                                                          new ParameterInjection("nameServerList", nameServerAddresses),
                                                                                                          new ParameterInjection("defaultPort", defaultPort)))
                      .Register<IMessageQueueClient, MessageQueueClient>(ServiceLifetime.Singleton);

            return configuration;
        }
    }
}