using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue
{
    public delegate void OnMessagesReceived(CancellationToken cancellationToken, params IMessageContext[] messageContext);

    public interface IMessageQueueClient : IDisposable
    {
        Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken);
        Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken);

        IMessageContext WrapMessage(object message,
                                    string correlationId = null,
                                    string topic = null,
                                    string key = null,
                                    string replyEndPoint = null,
                                    string messageId = null,
                                    SagaInfo sagaInfo = null,
                                    string producer = null);

        IMessageConsumer StartSubscriptionClient(string[] topics,
                                                 string subscriptionName,
                                                 string consumerId,
                                                 OnMessagesReceived onMessagesReceived,
                                                 ConsumerConfig consumerConfig = null,
                                                 IMessageContextBuilder messageContextBuilder = null);

        IMessageConsumer StartSubscriptionClient(string topic,
                                                 string subscriptionName,
                                                 string consumerId,
                                                 OnMessagesReceived onMessagesReceived,
                                                 ConsumerConfig consumerConfig = null,
                                                 IMessageContextBuilder messageContextBuilder = null);

        IMessageConsumer StartQueueClient(string commandQueueName,
                                          string consumerId,
                                          OnMessagesReceived onMessagesReceived,
                                          ConsumerConfig consumerConfig = null,
                                          IMessageContextBuilder messageContextBuilder = null);
    }
}