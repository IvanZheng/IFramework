using System;
using System.Collections.Generic;
using Kafka.Client.Messages;
using Kafka.Client.Responses;

namespace Kafka.Client.Producers
{
    public class ProduceDispatchSeralizeResult<K>
    {
        public ProduceDispatchSeralizeResult(IEnumerable<Exception> exceptions,
            IEnumerable<ProducerData<K, Message>> failedProducerDatas,
            List<Tuple<int, TopicAndPartition, ProducerResponseStatus>> failedDetail, bool hasDataNeedDispatch)
        {
            Exceptions = exceptions;
            FailedProducerDatas = failedProducerDatas;
            FailedDetail = failedDetail;
            HasDataNeedDispatch = hasDataNeedDispatch;
        }

        public IEnumerable<ProducerData<K, Message>> FailedProducerDatas { get; }
        public IEnumerable<Exception> Exceptions { get; }
        public List<Tuple<int, TopicAndPartition, ProducerResponseStatus>> FailedDetail { get; }
        public bool HasDataNeedDispatch { get; }
    }
}