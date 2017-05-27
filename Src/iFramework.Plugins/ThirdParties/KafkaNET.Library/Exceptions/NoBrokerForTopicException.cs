using System.Runtime.Serialization;

namespace Kafka.Client.Exceptions
{
    public class NoBrokerForTopicException : KafkaClientException
    {
        public NoBrokerForTopicException()
        {
        }

        public NoBrokerForTopicException(string message) : base(message)
        {
        }

        public NoBrokerForTopicException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}