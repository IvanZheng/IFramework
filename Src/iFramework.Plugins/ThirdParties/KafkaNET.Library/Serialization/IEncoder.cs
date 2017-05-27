using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    /// <summary>
    ///     User-defined serializer to <see cref="Message" /> format
    /// </summary>
    /// <typeparam name="TData">
    ///     Type od data
    /// </typeparam>
    public interface IEncoder<TData>
    {
        /// <summary>
        ///     Serializes given data to <see cref="Message" /> format
        /// </summary>
        /// <param name="data">
        ///     The data to serialize.
        /// </param>
        /// <returns>
        ///     Serialized data
        /// </returns>
        Message ToMessage(TData data);
    }
}