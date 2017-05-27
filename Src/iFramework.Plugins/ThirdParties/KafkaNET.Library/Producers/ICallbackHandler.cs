using System;
using System.Collections.Generic;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     Performs action when a producer request is finished being sent asynchronously.
    /// </summary>
    public interface ICallbackHandler<K, V> : IDisposable
    {
        /// <summary>
        ///     Performs action when a producer request is finished being sent asynchronously.
        /// </summary>
        /// <param name="events">
        ///     The sent request events.
        /// </param>
        void Handle(IEnumerable<ProducerData<K, V>> events);
    }
}