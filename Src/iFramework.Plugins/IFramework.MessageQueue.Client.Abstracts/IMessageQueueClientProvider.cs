using System;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public interface IMessageQueueClientProvider: IDisposable
    {
        IMessageContext WrapMessage(object message,
                                    string correlationId = null,
                                    string topic = null,
                                    string key = null,
                                    string replyEndPoint = null,
                                    string messageId = null,
                                    SagaInfo sagaInfo = null,
                                    string producer = null);

        IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                             OnMessagesReceived onMessagesReceived,
                                             string consumerId,
                                             ConsumerConfig consumerConfig,
                                             bool start = true);

        IMessageConsumer CreateTopicSubscription(string[] topics,
                                                 string subscriptionName,
                                                 OnMessagesReceived onMessagesReceived,
                                                 string consumerId,
                                                 ConsumerConfig consumerConfig,
                                                 bool start = true);

        IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null);
        IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null);
    }
}