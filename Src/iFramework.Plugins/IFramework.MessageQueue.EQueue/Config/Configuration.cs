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

namespace IFramework.Config
{
    public static class ConfigurationEQueue
    {
        public static Configuration UseEQueue(this Configuration configuration, string brokerAddress,
                                            int producerPort = 5000, int consumerPort = 5001, int adminPort = 5002)
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


            IoCFactory.Instance.CurrentContainer
                     .RegisterType<IMessageQueueClient, EQueueClient>(Lifetime.Singleton,
                       new ConstructInjection(new ParameterInjection("brokerAddress", brokerAddress),
                       new ParameterInjection("producerPort", producerPort),
                       new ParameterInjection("consumerPort", consumerPort),
                       new ParameterInjection("adminPort", adminPort)));

            return configuration;
        }

        public static Configuration StartEqueueBroker(this Configuration configuration, int producerPort = 5000, int consumerPort = 5001, int adminPort = 5002)
        {
            var setting = new BrokerSetting(
               bool.Parse(ConfigurationManager.AppSettings["isMemoryMode"]),
               ConfigurationManager.AppSettings["fileStoreRootPath"],
               chunkCacheMaxPercent: 95,
               chunkFlushInterval: int.Parse(ConfigurationManager.AppSettings["flushInterval"]),
               messageChunkDataSize: int.Parse(ConfigurationManager.AppSettings["chunkSize"]) * 1024 * 1024,
               chunkWriteBuffer: int.Parse(ConfigurationManager.AppSettings["chunkWriteBuffer"]) * 1024,
               enableCache: bool.Parse(ConfigurationManager.AppSettings["enableCache"]),
               chunkCacheMinPercent: int.Parse(ConfigurationManager.AppSettings["chunkCacheMinPercent"]),
               messageChunkLocalCacheSize: 30 * 10000,
               queueChunkLocalCacheSize: 10000)
            {
                AutoCreateTopic = true,
                ProducerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), producerPort),
                ConsumerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), consumerPort),
                AdminAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), adminPort),
                NotifyWhenMessageArrived = true,
                MessageWriteQueueThreshold = int.Parse(ConfigurationManager.AppSettings["messageWriteQueueThreshold"])
            };
            BrokerController.Create(setting).Start();
            return configuration;
        }
    }
}
