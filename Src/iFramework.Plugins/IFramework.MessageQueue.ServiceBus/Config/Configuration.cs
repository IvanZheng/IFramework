using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using System.Collections.Generic;

namespace IFramework.MessageQueue.ServiceBus
{
    public static class IFrameworkConfigurationExtension
    {
        static IDictionary<string, int> QueuePartions
        {
            get; set;
        }

        public static int GetQueuePartionCount(this Configuration configuration, string queue)
        {
            int partionCount = 0;
            if (QueuePartions != null)
            {
                partionCount = QueuePartions.TryGetValue(queue, 0);
            }
            return partionCount;
        }


        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration SetQueuePartions(this Configuration configuration, IDictionary<string, int> queuePartions)
        {
            QueuePartions = queuePartions;
            return configuration;
        }
    }
}
