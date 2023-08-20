using Confluent.Kafka;
using IFramework.Message;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public interface IKafkaMessageContextBuilder: IMessageContextBuilder
    {
        IMessageContext Build(ConsumeResult<string, string> consumeResult);
    }
}
