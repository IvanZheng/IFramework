using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue
{
    public class MockMessageQueueClient : IMessageQueueClient
    {
        public void Dispose() { }

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  int fullLoadThreshold = 1000,
                                                  int waitInterval = 1000)
        {
            return null;
        }

        public ICommitOffsetable StartSubscriptionClient(string topic,
                                                         string subscriptionName,
                                                         string consumerId,
                                                         OnMessagesReceived onMessagesReceived,
                                                         int fullLoadThreshold = 1000,
                                                         int waitInterval = 1000)
        {
            return null;
        }

        public Task SendAsync(IMessageContext messageContext, string queue)
        {
            return Task.FromResult<object>(null);
        }

        public Task PublishAsync(IMessageContext messageContext, string topic)
        {
            return Task.FromResult<object>(null);
        }

        public IMessageContext WrapMessage(object message,
                                           string correlationId = null,
                                           string topic = null,
                                           string key = null,
                                           string replyEndPoint = null,
                                           string messageId = null,
                                           SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            return new EmptyMessageContext();
        }
    }
}