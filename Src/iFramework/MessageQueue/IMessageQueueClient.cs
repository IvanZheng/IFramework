using IFramework.Message;
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
                                    string replyEndPoint = null, string messageId = null);

        Action<IMessageContext> StartSubscriptionClient(string topic, string subscriptionName, string consuemrId, OnMessagesReceived onMessageReceived, int fullLoadThreshold = 1000, int waitInterval = 1000);

        //void StopSubscriptionClients();

        Action<IMessageContext> StartQueueClient(string commandQueueName, string consuemrId, OnMessagesReceived onMessageReceived, int fullLoadThreshold = 1000, int waitInterval = 1000);

        //void StopQueueClients();
    }
}
