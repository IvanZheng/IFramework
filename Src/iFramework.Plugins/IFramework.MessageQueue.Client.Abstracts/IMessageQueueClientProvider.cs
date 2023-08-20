using System;

namespace IFramework.MessageQueue.Client.Abstracts
{
    public interface IMessageQueueClientProvider : IDisposable
    {
        IMessageConsumer CreateQueueConsumer(string queue,
                                             OnMessagesReceived onMessagesReceived,
                                             string consumerId,
                                             ConsumerConfig config,
                                             bool start = true,
                                             IMessageContextBuilder messageContextBuilder = null);


        IMessageConsumer CreateTopicSubscription(string[] topics,
                                                 string subscriptionName,
                                                 OnMessagesReceived onMessagesReceived,
                                                 string consumerId,
                                                 ConsumerConfig consumerConfig,
                                                 bool start = true,
                                                 IMessageContextBuilder messageContextBuilder = null);

        IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null);
        IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null);
    }
}