using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public interface IMessageQueueClientProvider
    {
        IMessageContext WrapMessage(object message,
                                    string correlationId = null,
                                    string topic = null,
                                    string key = null,
                                    string replyEndPoint = null,
                                    string messageId = null,
                                    SagaInfo sagaInfo = null,
                                    string producer = null);
        IMessageConsumer CreateQueueConsumer(string commandQueueName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig);
        IMessageConsumer CreateTopicSubscription(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig);
        IMessageProducer CreateTopicProducer(string topic);
        IMessageProducer CreateQueueProducer(string queue);
    }
}
