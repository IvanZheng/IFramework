using Kafka.Client.Cfg;
using Kafka.Client.Messages;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     High-level Producer API that exposes all the producer functionality to the client
    ///     using <see cref="System.String" /> as type of key and <see cref="Message" /> as type of data
    /// </summary>
    public class Producer : Producer<string, Message>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Producer" /> class.
        /// </summary>
        /// <param name="config">The config object.</param>
        /// <remarks>
        ///     Can be used when all config parameters will be specified through the config object
        ///     and will be instantiated via reflection
        /// </remarks>
        public Producer(ProducerConfiguration config)
            : base(config) { }
    }
}