using Kafka.Client.Utils;

namespace Kafka.Client.Cfg
{
    public class SyncProducerConfiguration : ISyncProducerConfigShared
    {
        public const int DefaultBufferSize = 100 * 1024;

        public const int DefaultConnectTimeout = 5 * 1000;

        public const int DefaultReceiveTimeout = 5 * 1000;

        public const int DefaultSendTimeout = 5 * 1000;

        public const int DefaultReconnectInterval = 30 * 1000;

        public const int DefaultMaxMessageSize = 1024 * 1024;

        public const int DefaultCorrelationId = -1;

        public const string DefaultClientId = "";

        public const short DefaultRequiredAcks = 0;

        public const int DefaultAckTimeout = 300;

        public SyncProducerConfiguration()
        {
            BufferSize = DefaultBufferSize;
            ConnectTimeout = DefaultConnectTimeout;
            MaxMessageSize = DefaultMaxMessageSize;
            CorrelationId = DefaultCorrelationId;
            ClientId = DefaultClientId;
            RequiredAcks = DefaultRequiredAcks;
            AckTimeout = DefaultAckTimeout;
            ReconnectInterval = DefaultReconnectInterval;
            ReceiveTimeout = DefaultReceiveTimeout;
            SendTimeout = DefaultSendTimeout;
        }

        public SyncProducerConfiguration(ProducerConfiguration config, int id, string host, int port)
        {
            Guard.NotNull(config, "config");

            Host = host;
            Port = port;
            BrokerId = id;
            BufferSize = config.BufferSize;
            ConnectTimeout = config.ConnectTimeout;
            MaxMessageSize = config.MaxMessageSize;
            ReconnectInterval = config.ReconnectInterval;
            ReceiveTimeout = config.ReceiveTimeout;
            SendTimeout = config.SendTimeout;
            ClientId = config.ClientId;
            CorrelationId = DefaultCorrelationId;
            RequiredAcks = config.RequiredAcks;
            AckTimeout = config.AckTimeout;
        }

        public int KeepAliveTime { get; set; }

        public int KeepAliveInterval { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public int BrokerId { get; set; }

        public int CorrelationId { get; set; }

        public int BufferSize { get; set; }

        public int ConnectTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public int SendTimeout { get; set; }

        public int MaxMessageSize { get; set; }

        public string ClientId { get; set; }

        public short RequiredAcks { get; set; }

        public int AckTimeout { get; set; }

        public int ReconnectInterval { get; set; }
    }
}