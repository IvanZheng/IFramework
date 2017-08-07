using IFramework.Config;
using IFramework.IoC;

namespace IFramework.MessageQueue.ConfluentKafka.Config
{
    public static class FrameworkConfigurationExtension
    {
        public static Configuration UseConfluentKafka(this Configuration configuration,
                                                      string brokerList)
        {
            IoCFactory.Instance.CurrentContainer
                      .RegisterType<IMessageQueueClient, ConfluentKafkaClient>(Lifetime.Singleton,
                                                                               new ConstructInjection(new ParameterInjection("brokerList", brokerList)));
            return configuration;
        }
    }
}