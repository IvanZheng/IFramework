using System.Collections.Generic;
using Kafka.Client.Cluster;

namespace Kafka.Client.Producers.Partitioning
{
    /// <summary>
    ///     Retrieves brokers and partitions info
    /// </summary>
    public interface IBrokerPartitionInfo
    {
        void UpdateInfo(short versionId, int correlationId, string clientId, string topic);
        List<Partition> GetBrokerPartitionInfo(short versionId, string clientId, int correlationId, string topic);

        IDictionary<int, Broker> GetBrokerPartitionLeaders(short versionId, string clientId, int correlationId,
            string topic);

        List<Partition> GetBrokerPartitionInfo(string topic);
        IDictionary<int, Broker> GetBrokerPartitionLeaders(string topic);
    }
}