using System.Collections.Generic;
using Kafka.Client.Serialization;

namespace Kafka.Client.Consumers
{
    public interface IZookeeperConsumerConnector : IConsumerConnector
    {
        string ConsumerGroup { get; }

        void AutoCommit();

        string GetConsumerIdString();

        IDictionary<string, IList<IKafkaMessageStream<TData>>> CreateMessageStreams<TData>(
            IDictionary<string, int> topicCountDict, IDecoder<TData> decoder);

        IDictionary<string, IDictionary<int, PartitionTopicInfo>> GetCurrentOwnership();

        void ReleaseAllPartitionOwnerships();
    }
}