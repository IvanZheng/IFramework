using System;

namespace Kafka.Client
{
    /// <summary>
    ///     Base class for all Kafka clients
    /// </summary>
    public abstract class KafkaClientBase : IDisposable
    {
        /// <summary>
        ///     Releases all unmanaged and managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases all unmanaged and managed resources
        /// </summary>
        /// <param name="disposing">
        ///     Indicates whether release managed resources.
        /// </param>
        protected abstract void Dispose(bool disposing);
    }
}