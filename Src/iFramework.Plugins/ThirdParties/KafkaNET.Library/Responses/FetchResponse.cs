using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Responses
{
    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class FetchResponse
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(FetchResponse));

        public FetchResponse(int correlationId, IEnumerable<TopicData> data)
        {
            Guard.NotNull(data, "data");
            CorrelationId = correlationId;
            TopicDataDict = data.GroupBy(x => x.Topic, x => x)
                .ToDictionary(x => x.Key, x => x.ToList().FirstOrDefault());
        }

        public FetchResponse(int correlationId, IEnumerable<TopicData> data, int size)
        {
            Guard.NotNull(data, "data");
            CorrelationId = correlationId;
            TopicDataDict = data.GroupBy(x => x.Topic, x => x)
                .ToDictionary(x => x.Key, x => x.ToList().FirstOrDefault());
            Size = size;
        }

        public int Size { get; }
        public int CorrelationId { get; }
        public Dictionary<string, TopicData> TopicDataDict { get; }

        public BufferedMessageSet MessageSet(string topic, int partition)
        {
            var messageSet = new BufferedMessageSet(Enumerable.Empty<Message>(), partition);
            if (TopicDataDict.ContainsKey(topic))
            {
                var topicData = TopicDataDict[topic];
                if (topicData != null)
                {
                    var data = TopicData.FindPartition(topicData.PartitionData, partition);
                    if (data != null)
                    {
                        messageSet = new BufferedMessageSet(data.MessageSet.Messages, (short) data.Error, partition);
                        messageSet.HighwaterOffset = data.HighWaterMark;
                    }
                    else
                    {
                        Logger.WarnFormat("Partition data was not found for partition {0}.", partition);
                    }
                }
            }

            return messageSet;
        }

        public PartitionData PartitionData(string topic, int partition)
        {
            if (TopicDataDict.ContainsKey(topic))
            {
                var topicData = TopicDataDict[topic];
                if (topicData != null)
                    return TopicData.FindPartition(topicData.PartitionData, partition);
            }

            return new PartitionData(partition, new BufferedMessageSet(Enumerable.Empty<Message>(), partition));
        }

        public class Parser : IResponseParser<FetchResponse>
        {
            public FetchResponse ParseFrom(KafkaBinaryReader reader)
            {
                int size = 0, correlationId = 0, dataCount = 0;
                try
                {
                    size = reader.ReadInt32();
                    correlationId = reader.ReadInt32();
                    dataCount = reader.ReadInt32();
                    var data = new TopicData[dataCount];
                    for (var i = 0; i < dataCount; i++)
                        data[i] = TopicData.ParseFrom(reader);

                    return new FetchResponse(correlationId, data, size);
                }
                catch (OutOfMemoryException mex)
                {
                    Logger.Error(string.Format(
                        "OOM Error. Data values were: size: {0}, correlationId: {1}, dataCound: {2}.\r\nFull Stack of exception: {3}",
                        size, correlationId, dataCount, mex.StackTrace));
                    throw;
                }
            }
        }
    }
}