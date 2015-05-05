using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Send(IMessageContext messageContext, string queue);
        void Publish(IMessageContext messageContext, string topic);

        void CloseTopicClients();

        IMessageContext WrapMessage(object message, string correlationId = null, string topic = null);

        void StartSubscriptionClient(string topic, string subscriptionName, Action<IMessageContext> onMessageReceived);

        void StopSubscriptionClients();

        void StartQueueClient(string commandQueueName, Action<IMessageContext> onMessageReceived);

        void StopQueueClients();
    }
}
