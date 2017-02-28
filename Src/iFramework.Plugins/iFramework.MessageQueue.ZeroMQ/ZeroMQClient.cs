using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Message;
using IFramework.Message.Impl;

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

        public ICommitOffsetable StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            throw new NotImplementedException();
        }

        public ICommitOffsetable StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null)
        {
            throw new NotImplementedException();
        }
    }
}
