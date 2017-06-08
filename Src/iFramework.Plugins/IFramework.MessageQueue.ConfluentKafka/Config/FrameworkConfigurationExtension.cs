using IFramework.Config;
using IFramework.IoC;

namespace IFramework.MessageQueue.ConfluentKafka.Config
{
    public static class FrameworkConfigurationExtension
    {
        private static int _backOffIncrement = 30;

        public static Configuration UseConfluentKafka(this Configuration configuration,
                                                      string brokerlist,
                                                      int backOffIncrement = 30)
        {
            IoCFactory.Instance.CurrentContainer
                      .RegisterType<IMessageQueueClient, ConfluentKafkaClient>(Lifetime.Singleton,
                                                                               new ConstructInjection(new ParameterInjection("brokerList", brokerlist)));
            _backOffIncrement = backOffIncrement;
            return configuration;
        }

        public static int GetBackOffIncrement(this Configuration configuration)
        {
            return _backOffIncrement;
        }
    }
}