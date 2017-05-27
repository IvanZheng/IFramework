using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Kafka.Client.Requests;

namespace Kafka.Client.Cfg
{
    /// <summary>
    ///     Configuration used by the simple consumer api and consumer group.
    ///     When use consumer group, need tune MaxFetchBufferLength, BufferSize, FetchSize according to different message size.
    /// </summary>
    public class ConsumerConfiguration
    {
        public const short DefaultNumberOfTries = 2;

        public const int DefaultTimeout = -1;

        public const int DefaultShutdownTimeout = 10000;

        public const bool DefaultAutoCommit = true;

        public const int DefaultAutoCommitInterval = 10 * 1000;

        //fetch.message.max.bytes
        public const int DefaultFetchSize = 11 * 1024 * 1024;

        //fetch.min.bytes
        public const int DefaultFetchMinBytes = 1;

        //fetch.wait.max.ms
        public const int DefaultMaxFetchWaitMs = 100;

        public const int DefaultMaxFetchFactor = 10;

        public const int DefaultBackOffIncrement = 1000;

        public const int DefaultSocketTimeout = 30 * 1000;

        //socket.receive.buffer.bytes
        public const int DefaultBufferSize = 11 * 1024 * 1024;

        public const int DefaultSendTimeout = 5 * 1000;

        public const int DefaultReceiveTimeout = 5 * 1000;

        public const int DefaultReconnectInterval = 60 * 1000;

        public const string DefaultConsumerId = null;

        public const string DefaultSection = "kafkaConsumer";

        public const int DefaultMaxFetchBufferLength = 1000;

        public const int DefaultConsumeGroupRebalanceRetryIntervalMs = 1000;

        public const int DefaultConsumeGroupFindNewLeaderSleepIntervalMs = 2000;

        /// <summary>
        ///     The number of retry for get response.
        ///     Default value: 2
        /// </summary>
        public short NumberOfTries { get; set; }

        /// <summary>
        ///     The Socket send timeout. in milliseconds.
        ///     Default value 30*1000
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        ///     The Socket recieve time out. in milliseconds.
        ///     Default value 30*1000
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        ///     The Socket reconnect interval. in milliseconds.
        ///     Default value 60*1000
        /// </summary>
        public int ReconnectInterval { get; set; }

        /// <summary>
        ///     The socket recieve / send buffer size. in bytes.
        ///     Map to socket.receive.buffer.bytes in java api.
        ///     Default value 11 * 1024 * 1024
        ///     java version original default  value: 64 *1024
        /// </summary>
        public int BufferSize { get; set; }


        /// <summary>
        ///     Broker:  BrokerID, Host, Port
        /// </summary>
        public BrokerConfiguration Broker { get; set; }

        /// <summary>
        ///     Log level is verbose or not
        /// </summary>
        public bool Verbose { get; set; }

        private static void Validate(ConsumerConfigurationSection config)
        {
            if (config.Broker.ElementInformation.IsPresent
                && config.ZooKeeperServers.ElementInformation.IsPresent)
                throw new ConfigurationErrorsException(
                    "ZooKeeper configuration cannot be set when brokers configuration is used");

            if (!config.ZooKeeperServers.ElementInformation.IsPresent
                && !config.Broker.ElementInformation.IsPresent)
                throw new ConfigurationErrorsException("ZooKeeper server or Kafka broker configuration must be set");

            if (config.ZooKeeperServers.ElementInformation.IsPresent
                && config.ZooKeeperServers.Servers.Count == 0)
                throw new ConfigurationErrorsException("At least one ZooKeeper server address is required");
        }

        private static string GetIpAddress(string host)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(host, out ipAddress))
            {
                var ip = Dns.GetHostEntry(host);
                if (ip.AddressList.Length > 0)
                    return ip.AddressList[0].ToString();

                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                    "Could not resolve the zookeeper server address: {0}.", host));
            }

            return host;
        }

        private void SetBrokerConfiguration(BrokerConfigurationElement config)
        {
            Broker = new BrokerConfiguration
            {
                BrokerId = config.Id,
                Host = GetIpAddress(config.Host),
                Port = config.Port
            };
        }

        private void SetZooKeeperConfiguration(ZooKeeperConfigurationElement config)
        {
            if (config.Servers.Count == 0)
                throw new ConfigurationErrorsException();

            var sb = new StringBuilder();
            foreach (ZooKeeperServerConfigurationElement server in config.Servers)
            {
                sb.Append(GetIpAddress(server.Host));
                sb.Append(':');
                sb.Append(server.Port);
                sb.Append(',');
            }

            sb.Remove(sb.Length - 1, 1);
            ZooKeeper = new ZooKeeperConfiguration(
                sb.ToString(),
                config.SessionTimeout,
                config.ConnectionTimeout,
                config.SyncTime);
        }

        #region Constructor

        public ConsumerConfiguration()
        {
            NumberOfTries = DefaultNumberOfTries;
            Timeout = DefaultTimeout;
            AutoOffsetReset = OffsetRequest.SmallestTime;
            AutoCommit = DefaultAutoCommit;
            AutoCommitInterval = DefaultAutoCommitInterval;
            FetchSize = DefaultFetchSize;
            FetchMinBytes = DefaultFetchMinBytes;
            MaxFetchWaitMs = DefaultMaxFetchWaitMs;
            MaxFetchFactor = DefaultMaxFetchFactor;
            BackOffIncrement = DefaultBackOffIncrement;
            ConsumerId = GetHostName();
            ReconnectInterval = DefaultReconnectInterval;
            ShutdownTimeout = DefaultShutdownTimeout;
            MaxFetchBufferLength = DefaultMaxFetchBufferLength;
            SendTimeout = DefaultSocketTimeout;
            ReceiveTimeout = DefaultSocketTimeout;
            BufferSize = DefaultBufferSize;
            Verbose = false;
            ConsumeGroupRebalanceRetryIntervalMs = DefaultConsumeGroupRebalanceRetryIntervalMs;
            ConsumeGroupFindNewLeaderSleepIntervalMs = DefaultConsumeGroupFindNewLeaderSleepIntervalMs;
        }

        private string GetHostName()
        {
            var shortHostName = Dns.GetHostName();
            var fullHostName = Dns.GetHostEntry(shortHostName).HostName;
            return fullHostName;
        }

        public ConsumerConfiguration(string host, int port)
            : this()
        {
            Broker = new BrokerConfiguration {Host = host, Port = port};
        }

        public ConsumerConfiguration(ConsumerConfigurationSection config)
        {
            Validate(config);
            NumberOfTries = config.NumberOfTries;
            GroupId = config.GroupId;
            Timeout = config.Timeout;
            AutoOffsetReset = config.AutoOffsetReset;
            AutoCommit = config.AutoCommit;
            AutoCommitInterval = config.AutoCommitInterval;
            FetchSize = config.FetchSize;
            BackOffIncrement = config.BackOffIncrement;
            SendTimeout = config.SendTimeout;
            ReceiveTimeout = config.ReceiveTimeout;
            BufferSize = config.BufferSize;
            ConsumerId = config.ConsumerId;
            ShutdownTimeout = config.ShutdownTimeout;
            MaxFetchBufferLength = config.MaxFetchBufferLength;
            ConsumeGroupRebalanceRetryIntervalMs = DefaultConsumeGroupRebalanceRetryIntervalMs;
            ConsumeGroupFindNewLeaderSleepIntervalMs = DefaultConsumeGroupFindNewLeaderSleepIntervalMs;
            if (config.Broker.ElementInformation.IsPresent)
                SetBrokerConfiguration(config.Broker);
            else
                SetZooKeeperConfiguration(config.ZooKeeperServers);
        }

        public ConsumerConfiguration(XElement xmlElement)
            : this(ConsumerConfigurationSection.FromXml(xmlElement))
        {
        }

        public ConsumerConfiguration(ConsumerConfiguration cosumerConfigTemplate,
            BrokerConfiguration brokerConfiguration)
        {
            Broker = brokerConfiguration;

            NumberOfTries = cosumerConfigTemplate.NumberOfTries;
            Timeout = cosumerConfigTemplate.Timeout;
            AutoOffsetReset = cosumerConfigTemplate.AutoOffsetReset;
            AutoCommit = cosumerConfigTemplate.AutoCommit;
            AutoCommitInterval = cosumerConfigTemplate.AutoCommitInterval;
            FetchSize = cosumerConfigTemplate.FetchSize;
            MaxFetchFactor = cosumerConfigTemplate.MaxFetchFactor;
            BackOffIncrement = cosumerConfigTemplate.BackOffIncrement;
            ConsumerId = cosumerConfigTemplate.ConsumerId;
            ReconnectInterval = cosumerConfigTemplate.ReconnectInterval;
            ShutdownTimeout = cosumerConfigTemplate.ShutdownTimeout;
            MaxFetchBufferLength = cosumerConfigTemplate.MaxFetchBufferLength;
            SendTimeout = cosumerConfigTemplate.SendTimeout;
            ReceiveTimeout = cosumerConfigTemplate.ReceiveTimeout;
            BufferSize = cosumerConfigTemplate.BufferSize;
            Verbose = cosumerConfigTemplate.Verbose;
            ConsumeGroupRebalanceRetryIntervalMs = cosumerConfigTemplate.ConsumeGroupRebalanceRetryIntervalMs;
            ConsumeGroupFindNewLeaderSleepIntervalMs = cosumerConfigTemplate.ConsumeGroupFindNewLeaderSleepIntervalMs;
        }

        public static ConsumerConfiguration Configure(string section)
        {
            var config = ConfigurationManager.GetSection(section) as ConsumerConfigurationSection;
            return new ConsumerConfiguration(config);
        }

        #endregion

        #region Consumer Group API only

        /// <summary>
        ///     Consumer group API only.
        ///     the number of byes of messages to attempt to fetch.
        ///     map to fetch.message.max.bytes of java version.
        ///     Finally it call FileChannle.position(long newPosition)  and got to native call position0(FileDescriptor fd, long
        ///     offset)
        ///     Default value: 11 * 1024*1024
        /// </summary>
        public int FetchSize { get; set; }

        /// <summary>
        ///     fetch.min.bytes -
        ///     The minimum amount of data the server should return for a fetch request. If insufficient data is available the
        ///     request will wait for that much data to accumulate before answering the request.
        ///     Default value: 1
        /// </summary>
        public int FetchMinBytes { get; set; }

        /// <summary>
        ///     fetch.wait.max.ms -
        ///     The maximum amount of time the server will block before answering the fetch request if there isn't sufficient data
        ///     to immediately satisfy fetch.min.bytes.
        ///     Default value: 100
        /// </summary>
        public int MaxFetchWaitMs { get; set; }

        /// <summary>
        ///     Consumer Group API only. Zookeeper
        /// </summary>
        public ZooKeeperConfiguration ZooKeeper { get; set; }

        /// <summary>
        ///     Consumer Group API only.  The group name of consumer group, should not be empty.
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        ///     Consumer Group API only. The time out of get data from the BlockingCollection of KafkaMessageStream. in
        ///     milliseconds.
        ///     If the value less than 0, it will block there is no data available.
        ///     If the value bigger of equal than 0 and got time out , one ConsumerTimeoutException will be thrown.
        ///     Default value: -1
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        ///     Consumer Group API only.  The time out of shutting down fetcher thread. in milliseconds.
        ///     Default value: 10,000
        /// </summary>
        public int ShutdownTimeout { get; set; }

        /// <summary>
        ///     Consumer Group API only. Where to reset offset after got ErrorMapping.OffsetOutOfRangeCode.
        ///     Valid value: OffsetRequest.SmallestTime  or OffsetRequest.LargestTime
        ///     Default value: OffsetRequest.SmallestTime
        /// </summary>
        public string AutoOffsetReset { get; set; }

        /// <summary>
        ///     Consumer Group API only.  Automate commit offset or not.
        ///     Default value: true
        /// </summary>
        public bool AutoCommit { get; set; }

        /// <summary>
        ///     Consumer Group API only.  The interval of commit offset. in milliseconds.
        ///     Default value: 10,000
        /// </summary>
        public int AutoCommitInterval { get; set; }

        /// <summary>
        ///     Consumer Group API only. The count of message trigger fetcher thread cache, if the message count in fetch thread
        ///     less than it, it will try fetch more from kafka.
        ///     Default value: 1000
        ///     Should be : (5~10 even 100) *  FetchSize / average message size
        ///     If this value set too big, your exe will use more memory to cache data.
        ///     If this valuse set too small, your exe will raise more request to Kafka.
        /// </summary>
        public int MaxFetchBufferLength { get; set; }

        /// <summary>
        ///     Consumer Group API only.  the time of sleep when no data to fetch. in milliseconds.
        ///     Default value: 1000
        /// </summary>
        public int BackOffIncrement { get; set; }

        /// <summary>
        ///     Consumer group only.
        ///     Default value: host name
        /// </summary>
        public string ConsumerId
        {
            get => consumerId;
            set => consumerId = value + "_" + DateTime.UtcNow.Ticks;
        }

        private string consumerId;

        /// <summary>
        ///     Consumer group only.
        ///     Default value : 1000 ms.
        /// </summary>
        public int ConsumeGroupRebalanceRetryIntervalMs { get; set; }

        /// <summary>
        ///     Consumer group only.
        ///     Default value: 2000ms
        /// </summary>
        public int ConsumeGroupFindNewLeaderSleepIntervalMs { get; set; }

        #endregion

        #region some obsolete fields

        /// <summary>
        ///     No place use it now.  Why it here?
        /// </summary>
        public int MaxFetchSize => FetchSize * MaxFetchFactor;

        /// <summary>
        ///     No place use it now.  Why it here?
        /// </summary>
        public int MaxFetchFactor { get; set; }

        #endregion
    }
}