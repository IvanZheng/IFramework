using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue
{
    public delegate void OnMessagesReceived(params IMessageContext[] messageContext);
    public interface IMessageQueueClient
    {
        void Send(IMessageContext messageContext, string queue);
        void Publish(IMessageContext messageContext, string topic);

        IMessageContext WrapMessage(object message, string correlationId = null,
                                    string topic = null, string key = null,
                                    string replyEndPoint = null, string messageId = null);

        Action<long> StartSubscriptionClient(string topic, int partition, string subscriptionName,  OnMessagesReceived onMessageReceived);

        void StopSubscriptionClients();

        Action<long> StartQueueClient(string commandQueueName, int partition, OnMessagesReceived onMessageReceived);

        void StopQueueClients();
    }
}
