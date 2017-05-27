using System.Collections.Generic;
using IFramework.Config;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ServiceBus
{
    public static class FrameworkConfigurationExtension
    {
        private static IDictionary<string, int> QueuePartitions { get; set; }

        public static int GetQueuePartitionCount(this Configuration configuration, string queue)
        {
            var partitionCount = 0;
            if (QueuePartitions != null)
                partitionCount = QueuePartitions.TryGetValue(queue, 0);
            return partitionCount;
        }


        /// <summary>
        ///     Use Log4Net as the logger for the enode framework.
        /// </summary>
        /// <returns></returns>
        public static Configuration SetQueuePartitions(this Configuration configuration,
            IDictionary<string, int> queuePartitions)
        {
            QueuePartitions = queuePartitions;
            return configuration;
        }
    }
}