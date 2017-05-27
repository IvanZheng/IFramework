using System.Runtime.Serialization;

namespace Kafka.Client.Cluster
{
    [DataContract]
    public class ZkBrokerInfo
    {
        [DataMember(Name = "host")] public string Host;
        [DataMember(Name = "jmx_port")] public int JmxPort;
        [DataMember(Name = "timestamp")] public long Timestamp;
        [DataMember(Name = "version")] public int version;
    }
}