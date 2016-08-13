using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Message;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class ZeroMQClient : IMessageQueueClient
    {
        public void Dispose()
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

        public Action<IMessageContext> StartQueueClient(string commandQueueName, string consuemrId, OnMessagesReceived onMessageReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            throw new NotImplementedException();
        }

        public Action<IMessageContext> StartSubscriptionClient(string topic, string subscriptionName, string consuemrId, OnMessagesReceived onMessageReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null)
        {
            throw new NotImplementedException();
        }
    }
}
