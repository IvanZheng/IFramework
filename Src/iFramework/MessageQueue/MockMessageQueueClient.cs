using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue
{
    public class MockMessageQueueClient : IMessageQueueClient
    {
        public void Dispose()
        {

        }

        public void Publish(IMessageContext messageContext, string topic)
        {

        }

        public void Send(IMessageContext messageContext, string queue)
        {

        }

        public ICommitOffsetable StartQueueClient(string commandQueueName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            return null;
        }

        public ICommitOffsetable StartSubscriptionClient(string topic, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, int fullLoadThreshold = 1000, int waitInterval = 1000)
        {
            return null;
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null)
        {
            return new EmptyMessageContext();
        }
    }
}
