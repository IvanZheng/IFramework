using System.Text;
using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    public class StringDecoder : IDecoder<string>
    {
        public string ToEvent(Message message)
        {
            var buffer = message.Payload;
            return Encoding.UTF8.GetString(buffer);
        }
    }
}