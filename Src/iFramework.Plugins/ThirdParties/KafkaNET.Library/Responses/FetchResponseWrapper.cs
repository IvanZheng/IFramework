using System.Collections.Generic;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Messages;

namespace Kafka.Client.Responses
{
    public class FetchResponseWrapper
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(FetchResponseWrapper));

        public FetchResponseWrapper(List<MessageAndOffset> list,
                                    int size,
                                    int correlationid,
                                    string topic,
                                    int partitionId)
        {
            MessageAndOffsets = list;
            Size = size;
            CorrelationId = correlationid;
            Topic = topic;
            PartitionId = partitionId;
            MessageCount = MessageAndOffsets.Count;
        }

        public int Size { get; }
        public int CorrelationId { get; }
        public List<MessageAndOffset> MessageAndOffsets { get; }
        public int MessageCount { get; set; }
        public string Topic { get; }
        public int PartitionId { get; }
    }
}