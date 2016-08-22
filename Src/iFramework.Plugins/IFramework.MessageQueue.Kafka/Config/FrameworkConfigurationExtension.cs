using IFramework.Config;
using IFramework.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka.Config
{
    public static class FrameworkConfigurationExtension
    {
        
        public static Configuration UseKafka(this Configuration configuration, string zkConnectionString, int backOffIncrement = 30)
        {
            IoCFactory.Instance.CurrentContainer
                      .RegisterType<IMessageQueueClient, KafkaClient>(Lifetime.Singleton,
                        new ConstructInjection(new ParameterInjection("zkConnectionString", zkConnectionString)));
            _backOffIncrement = backOffIncrement;
            return configuration;
        }

        static int _backOffIncrement = 30;
        public static int GetBackOffIncrement(this Configuration configuration)
        {
            return _backOffIncrement;
        }
    }
}
