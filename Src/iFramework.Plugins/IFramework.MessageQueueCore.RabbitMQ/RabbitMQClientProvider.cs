using System;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;

namespace IFramework.MessageQueueCore.RabbitMQ
{
    public class RabbitMQClientProvider : IMessageQueueClientProvider
    {
        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateTopicSubscription(string topic,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateTopicProducer(string topic)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateQueueProducer(string queue)
        {
            throw new NotImplementedException();
        }
    }
}