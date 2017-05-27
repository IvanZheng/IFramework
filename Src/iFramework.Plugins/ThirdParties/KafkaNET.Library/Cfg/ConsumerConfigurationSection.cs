using System.Configuration;
using System.Xml.Linq;
using Kafka.Client.Requests;

namespace Kafka.Client.Cfg
{
    public class ConsumerConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("numberOfTries", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultNumberOfTries)]
        public short NumberOfTries => (short) this["numberOfTries"];

        [ConfigurationProperty("groupId", IsRequired = true)]
        public string GroupId => (string) this["groupId"];

        [ConfigurationProperty("timeout", IsRequired = false, DefaultValue = ConsumerConfiguration.DefaultTimeout)]
        public int Timeout => (int) this["timeout"];

        [ConfigurationProperty("autoOffsetReset", IsRequired = false, DefaultValue = OffsetRequest.SmallestTime)]
        public string AutoOffsetReset => (string) this["autoOffsetReset"];

        [ConfigurationProperty("consumerId", IsRequired = false, DefaultValue = null)]
        public string ConsumerId => (string) this["consumerId"];

        [ConfigurationProperty("autoCommit", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultAutoCommit)]
        public bool AutoCommit => (bool) this["autoCommit"];

        [ConfigurationProperty("autoCommitInterval", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultAutoCommitInterval)]
        public int AutoCommitInterval => (int) this["autoCommitInterval"];

        [ConfigurationProperty("fetchSize", IsRequired = false, DefaultValue = ConsumerConfiguration.DefaultFetchSize)]
        public int FetchSize => (int) this["fetchSize"];

        [ConfigurationProperty("backOffIncrement", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultBackOffIncrement)]
        public int BackOffIncrement => (int) this["backOffIncrement"];

        [ConfigurationProperty("sendTimeout", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultSocketTimeout)]
        public int SendTimeout => (int) this["sendTimeout"];

        [ConfigurationProperty("receiveTimeout", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultSocketTimeout)]
        public int ReceiveTimeout => (int) this["receiveTimeout"];

        [ConfigurationProperty("bufferSize", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultSocketTimeout)]
        public int BufferSize => (int) this["bufferSize"];

        [ConfigurationProperty("maxFetchBufferLength", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultMaxFetchBufferLength)]
        public int MaxFetchBufferLength => (int) this["maxFetchBufferLength"];

        [ConfigurationProperty("shutdownTimeout", IsRequired = false,
            DefaultValue = ConsumerConfiguration.DefaultShutdownTimeout)]
        public int ShutdownTimeout => (int) this["shutdownTimeout"];

        [ConfigurationProperty("zookeeper", IsRequired = false, DefaultValue = null)]
        public ZooKeeperConfigurationElement ZooKeeperServers => (ZooKeeperConfigurationElement) this["zookeeper"];

        [ConfigurationProperty("broker", IsRequired = false)]
        public BrokerConfigurationElement Broker => (BrokerConfigurationElement) this["broker"];

        public static ConsumerConfigurationSection FromXml(XElement element)
        {
            var section = new ConsumerConfigurationSection();
            section.DeserializeSection(element.CreateReader());
            return section;
        }
    }
}