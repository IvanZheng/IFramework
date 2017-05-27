using System.IO;

namespace Kafka.Client.Messages
{
    /// <summary>
    ///     Wrapper over memory set with fixed capacity
    /// </summary>
    internal class BoundedBuffer : MemoryStream
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BoundedBuffer" /> class.
        /// </summary>
        /// <param name="size">
        ///     The max size of stream.
        /// </param>
        public BoundedBuffer(int size)
            : base(new byte[size], 0, size, true, true)
        {
        }
    }
}