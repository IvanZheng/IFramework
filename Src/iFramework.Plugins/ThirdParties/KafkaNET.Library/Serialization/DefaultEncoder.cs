using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    /// <summary>
    ///     Default serializer that expects <see cref="Message" /> object
    /// </summary>
    public class DefaultEncoder : IEncoder<Message>
    {
        /// <summary>
        ///     Do nothing with data
        /// </summary>
        /// <param name="data">
        ///     The data, that are already in <see cref="Message" /> format.
        /// </param>
        /// <returns>
        ///     Serialized data
        /// </returns>
        public Message ToMessage(Message data)
        {
            return data;
        }
    }
}