using System;
using System.Collections.Generic;

namespace Kafka.Client.Consumers
{
    public interface IConsumerIterator<TData> : IEnumerator<TData>, IDisposable
    {
        void ClearIterator();
    }
}