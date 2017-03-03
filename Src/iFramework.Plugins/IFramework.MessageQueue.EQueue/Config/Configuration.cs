using global::EQueue.Configurations;
using EQueue.Broker;
using ECommon.Configurations;
using ECommonConfiguration = ECommon.Configurations.Configuration;
using System.Configuration;
using System.Net;
using ECommon.Socketing;
using IFramework.IoC;
using IFramework.MessageQueue.EQueue;
using IFramework.MessageQueue;
using EQueue.NameServer;
using System.Collections.Generic;
using EQueue.Utils;
using System.Linq;
using System;
using EQueue.Clients.Producers;

namespace IFramework.Config
{
    public static class ConfigurationEQueue
    {
        static void InitializeEqueue()
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
            IoCFactory.Instance.CurrentContainer
                      .RegisterType<IMessageQueueClient, EQueueClient>(Lifetime.Singleton,
                       new ConstructInjection(new ParameterInjection("clusterName", clusterName),
                                              new ParameterInjection("nameAServerList", GetIPEndPoints(nameServerAddresses))
                       ));
            return configuration;
        }

        public static Configuration StartEqueueNameServer(this Configuration configuration)
        {
            new NameServerController().Start();
            return configuration;
        }


        public static IEnumerable<IPEndPoint> GetIPEndPoints(string addresses)
        {
            List<IPEndPoint> nameServerIPEndPoints = new List<IPEndPoint>();
            if (string.IsNullOrEmpty(addresses))
            {
                nameServerIPEndPoints.Add(new IPEndPoint(SocketUtils.GetLocalIPV4(), 9493));
            }
            else
            {
                foreach (var address in addresses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var segments = address.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length == 2)
                        {
                            nameServerIPEndPoints.Add(new IPEndPoint(IPAddress.Parse(segments[0]),
                                                                     int.Parse(segments[1])
                                                                    )
                                                     );
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return nameServerIPEndPoints;
        }


        public static Configuration StartEqueueBroker(this Configuration configuration, string clusterName = "DefaultCluster", string nameServerAddresses = null, int producerPort = 5000, int consumerPort = 5001, int adminPort = 5002)
        {

            var nameServerIPEndPoints = GetIPEndPoints(nameServerAddresses).ToList();
            if (nameServerIPEndPoints.Count == 0)
            {
                throw new Exception("no avaliable equeue name server address");
            }

            var setting = new BrokerSetting(
                bool.Parse(ConfigurationManager.AppSettings["isMemoryMode"]),
                ConfigurationManager.AppSettings["fileStoreRootPath"],
                chunkCacheMaxPercent: 95,
                chunkFlushInterval: int.Parse(ConfigurationManager.AppSettings["flushInterval"]),
                messageChunkDataSize: int.Parse(ConfigurationManager.AppSettings["chunkSize"]) * 1024 * 1024,
                chunkWriteBuffer: int.Parse(ConfigurationManager.AppSettings["chunkWriteBuffer"]) * 1024,
                enableCache: bool.Parse(ConfigurationManager.AppSettings["enableCache"]),
                chunkCacheMinPercent: int.Parse(ConfigurationManager.AppSettings["chunkCacheMinPercent"]),
                syncFlush: bool.Parse(ConfigurationManager.AppSettings["syncFlush"]),
                messageChunkLocalCacheSize: 30 * 10000,
                queueChunkLocalCacheSize: 10000)
            {
                NotifyWhenMessageArrived = bool.Parse(ConfigurationManager.AppSettings["notifyWhenMessageArrived"]),
                MessageWriteQueueThreshold = int.Parse(ConfigurationManager.AppSettings["messageWriteQueueThreshold"])
            };
            setting.BrokerInfo.ClusterName = clusterName;
            setting.NameServerList = nameServerIPEndPoints;
            setting.BrokerInfo.BrokerName = ConfigurationManager.AppSettings["brokerName"];
            setting.BrokerInfo.GroupName = ConfigurationManager.AppSettings["groupName"];
            setting.BrokerInfo.ProducerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), producerPort).ToString();
            setting.BrokerInfo.ConsumerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), consumerPort).ToString();
            setting.BrokerInfo.AdminAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), adminPort).ToString();
            BrokerController.Create(setting).Start();
            return configuration;
        }
    }
}
