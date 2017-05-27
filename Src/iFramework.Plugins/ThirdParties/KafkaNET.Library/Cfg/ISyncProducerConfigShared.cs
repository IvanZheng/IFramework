namespace Kafka.Client.Cfg
{
    internal interface ISyncProducerConfigShared
    {
        int BufferSize { get; set; }

        int ReceiveTimeout { get; set; }

        int SendTimeout { get; set; }

        int ConnectTimeout { get; set; }

        int ReconnectInterval { get; set; }

        int MaxMessageSize { get; set; }

        string ClientId { get; set; }

        short RequiredAcks { get; set; }

        int AckTimeout { get; set; }
    }
}