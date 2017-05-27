using System;
using System.Collections.Generic;
using Kafka.Client.Consumers;
using Kafka.Client.Producers;
using Kafka.Client.Requests;
using Kafka.Client.Responses;

namespace Kafka.Client
{
    public interface IKafkaConnection : IDisposable
    {
        FetchResponse Send(FetchRequest request);
        ProducerResponse Send(ProducerRequest request);
        OffsetResponse Send(OffsetRequest request);
        IEnumerable<TopicMetadata> Send(TopicMetadataRequest request);
    }
}