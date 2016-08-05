using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using System.Collections.Generic;

namespace IFramework.MessageQueue.ServiceBus
{
    public static class FrameworkConfigurationExtension
    {
        static IDictionary<string, int> QueuePartitions
        {
            get; set;
        }

        public static int GetQueuePartitionCount(this Configuration configuration, string queue)
        {
            int partitionCount = 0;
            if (QueuePartitions != null)
            {
                partitionCount = QueuePartitions.TryGetValue(queue, 0);
            }
            return partitionCount;
        }


        /// <summary>Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration SetQueuePartitions(this Configuration configuration, IDictionary<string, int> queuePartitions)
        {
            QueuePartitions = queuePartitions;
            return configuration;
        }
    }
}
