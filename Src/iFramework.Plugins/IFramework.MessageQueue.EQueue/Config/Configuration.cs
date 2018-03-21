using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using ECommon.Configurations;
using ECommon.Socketing;
using EQueue.Broker;
using EQueue.Configurations;
using EQueue.NameServer;
using IFramework.DependencyInjection;
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
            ECommonConfiguration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterUnhandledExceptionHandler()
                .RegisterEQueueComponents()
                .UseDeleteMessageByCountStrategy(10);
        }

        public static Configuration UseEQueue(this Configuration configuration,
                                              string nameServerAddresses = null,
                                              string clusterName = "DefaultCluster")
        {
            InitializeEqueue();

            IoCFactory.Instance
                      .ObjectProviderBuilder
                      .Register<IMessageQueueClientProvider, EQueueClientProvider>(ServiceLifetime.Singleton,
                                                                                   new ConstructInjection(new ParameterInjection("clusterName", clusterName),
                                                                                                          new ParameterInjection("nameServerList", GetIpEndPoints(nameServerAddresses))));
                     
            return configuration;
        }

        public static IEnumerable<IPEndPoint> GetIpEndPoints(string addresses)
        {
            var nameServerIpEndPoints = new List<IPEndPoint>();
            if (string.IsNullOrEmpty(addresses))
            {
                nameServerIpEndPoints.Add(new IPEndPoint(SocketUtils.GetLocalIPV4(), 9493));
            }
            else
            {
                foreach (var address in addresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var segments = address.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length == 2)
                        {
                            nameServerIpEndPoints.Add(new IPEndPoint(IPAddress.Parse(segments[0]),
                                                                     int.Parse(segments[1])
                                                                    )
                                                     );
                        }
                    }
                    catch (Exception) { }
                }
            }
            return nameServerIpEndPoints;
        }
    }
}