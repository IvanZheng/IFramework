using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue
{
    public delegate void OnMessagesReceived(params IMessageContext[] messageContext);
    public interface IMessageQueueClient : IDisposable
    {
        void Send(IMessageContext messageContext, string queue);
        void Publish(IMessageContext messageContext, string topic);

        IMessageContext WrapMessage(object message, string correlationId = null,
                                    string topic = null, string key = null,
                                    string replyEndPoint = null, string messageId = null,
                                    SagaInfo sagaInfo = null);

        ICommitOffsetable StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000);

        ICommitOffsetable StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000);
    }
}
