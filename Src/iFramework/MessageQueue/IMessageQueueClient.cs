using System;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue
{
    public delegate void OnMessagesReceived(params IMessageContext[] messageContext);

    public interface IMessageQueueClient : IDisposable
    {
        Task SendAsync(IMessageContext messageContext, string queue);
        Task PublishAsync(IMessageContext messageContext, string topic);

        IMessageContext WrapMessage(object message,
                                    string correlationId = null,
                                    string topic = null,
                                    string key = null,
                                    string replyEndPoint = null,
                                    string messageId = null,
                                    SagaInfo sagaInfo = null,
                                    string producer = null);

        ICommitOffsetable StartSubscriptionClient(string topic,
                                                  string subscriptionName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  int fullLoadThreshold = 1000,
                                                  int waitInterval = 1000);

        ICommitOffsetable StartQueueClient(string commandQueueName,
                                           string consumerId,
                                           OnMessagesReceived onMessagesReceived,
                                           int fullLoadThreshold = 1000,
                                           int waitInterval = 1000);
    }
}