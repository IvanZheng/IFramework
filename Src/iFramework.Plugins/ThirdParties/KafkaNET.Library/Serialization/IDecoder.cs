using Kafka.Client.Messages;

namespace Kafka.Client.Serialization
{
    public interface IDecoder<out TData>
    {
        TData ToEvent(Message message);
    }
}