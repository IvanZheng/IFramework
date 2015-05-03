using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue
{
    public interface IMessageQueueClient
    {
        void Publish(IMessageContext messageContext, string topic);

        void CloseTopicClients();

        IMessageContext WrapMessage(IMessage message);
      
        void StartSubscriptionClient(string topic, string _subscriptionName, Action<IMessageContext> OnMessageReceived);

        void StopSubscriptionClients();
    }
}
