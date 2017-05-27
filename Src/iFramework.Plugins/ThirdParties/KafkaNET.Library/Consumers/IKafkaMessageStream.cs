using System.Collections.Generic;
using System.Threading;

namespace Kafka.Client.Consumers
{
    public interface IKafkaMessageStream<TData> : IEnumerable<TData>
    {
        IConsumerIterator<TData> iterator { get; }
        int Count { get; }
        IKafkaMessageStream<TData> GetCancellable(CancellationToken cancellationToken);
        void Clear();
    }
}