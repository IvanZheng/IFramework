using System.Runtime.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Exceptions
{
    public class KafkaConsumeException : KafkaClientException
    {
        public KafkaConsumeException()
        {
        }

        public KafkaConsumeException(string message) : base(message)
        {
        }

        public KafkaConsumeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public KafkaConsumeException(string message, ErrorMapping errorCode)
            : base(message, errorCode)
        {
        }
    }
}