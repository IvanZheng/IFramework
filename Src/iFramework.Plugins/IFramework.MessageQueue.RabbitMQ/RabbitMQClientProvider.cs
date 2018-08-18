using IFramework.MessageQueue.Client.Abstracts;
using System;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class RabbitMQClientProvider: IMessageQueueClientProvider
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateTopicSubscription(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            throw new NotImplementedException();
        }
    }
}
