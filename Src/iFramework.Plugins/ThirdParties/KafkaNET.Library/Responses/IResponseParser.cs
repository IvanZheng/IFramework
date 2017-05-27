using Kafka.Client.Serialization;

namespace Kafka.Client.Responses
{
    public interface IResponseParser<out T>
    {
        T ParseFrom(KafkaBinaryReader reader);
    }
}