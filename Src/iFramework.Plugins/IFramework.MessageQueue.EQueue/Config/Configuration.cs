using global::EQueue.Configurations;
using EQueue.Broker;
using ECommon.Configurations;
using ECommonConfiguration = ECommon.Configurations.Configuration;
using System.Configuration;
using System.Net;
using ECommon.Socketing;

namespace IFramework.Config
{
    public static class ConfigurationEQueue
    {
        public static Configuration UseEQueue(this Configuration configuration)
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
                ProducerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), producerPort),
                ConsumerAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), consumerPort),
                AdminAddress = new IPEndPoint(SocketUtils.GetLocalIPV4(), adminPort),
                NotifyWhenMessageArrived = false,
                MessageWriteQueueThreshold = int.Parse(ConfigurationManager.AppSettings["messageWriteQueueThreshold"])
            };
            BrokerController.Create(setting).Start();
            return configuration;
        }
    }
}
