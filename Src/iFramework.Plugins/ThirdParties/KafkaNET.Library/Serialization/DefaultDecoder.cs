using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    public class DefaultDecoder : IDecoder<Message>
    {
        public Message ToEvent(Message message)
        {
            return message;
        }
    }
}