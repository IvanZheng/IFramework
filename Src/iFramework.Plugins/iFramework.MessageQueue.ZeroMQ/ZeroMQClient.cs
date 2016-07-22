using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Message;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class ZeroMQClient : IMessageQueueClient
    {
        public void CompleteMessage(IMessageContext messageContext)
        {
            throw new NotImplementedException();
        }

        public void Publish(IMessageContext messageContext, string topic)
        {
            throw new NotImplementedException();
        }

        public void Send(IMessageContext messageContext, string queue)
        {
            throw new NotImplementedException();
        }

        public Action<long> StartQueueClient(string commandQueueName, OnMessagesReceived onMessagesReceived)
        {
            throw new NotImplementedException();
        }

        public Action<long> StartSubscriptionClient(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived)
        {
            throw new NotImplementedException();
        }

        public void StopQueueClients()
        {
            throw new NotImplementedException();
        }

        public void StopSubscriptionClients()
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null)
        {
            throw new NotImplementedException();
        }

   
    }
}
