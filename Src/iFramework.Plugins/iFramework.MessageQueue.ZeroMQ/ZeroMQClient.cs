using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class ZeroMQClient : IMessageQueueClient
    {
        public void Publish(Message.IMessageContext messageContext, string topic)
        {
            throw new NotImplementedException();
        }

        public void CloseTopicClients()
        {
            throw new NotImplementedException();
        }

        public void StartSubscriptionClient(string topic, string _subscriptionName, Action<Message.IMessageContext> OnMessageReceived)
        {
            throw new NotImplementedException();
        }

        public void StopSubscriptionClients()
        {
            throw new NotImplementedException();
        }

        public void Send(Message.IMessageContext messageContext, string queue)
        {
            throw new NotImplementedException();
        }

        public Message.IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null)
        {
            throw new NotImplementedException();
        }

        public void StartQueueClient(string commandQueueName, Action<Message.IMessageContext> onMessageReceived)
        {
            throw new NotImplementedException();
        }

        public void StopQueueClients()
        {
            throw new NotImplementedException();
        }
    }
}
