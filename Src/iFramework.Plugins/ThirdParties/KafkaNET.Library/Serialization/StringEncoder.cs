using System.Text;
using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    /// <summary>
    ///     Serializes data to <see cref="Message" /> format using UTF-8 encoding
    /// </summary>
    public class StringEncoder : IEncoder<string>
    {
        /// <summary>
        ///     Serializes given data to <see cref="Message" /> format using UTF-8 encoding
        /// </summary>
        /// <param name="data">
        ///     The data to serialize.
        /// </param>
        /// <returns>
        ///     Serialized data
        /// </returns>
        public Message ToMessage(string data)
        {
            var encodedData = Encoding.UTF8.GetBytes(data);
            return new Message(encodedData);
        }
    }
}