using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue
{
    public class MockMessageQueueClient: IMessageQueueClient
    {
        public void Dispose()
        {
        }

        public Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }



        public Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public IMessageConsumer StartQueueClient(string commandQueueName, string consumerId,
                                                  OnMessagesReceived onMessagesReceived, ConsumerConfig consumerConfig = null)
        {
            return null;
        }

        public IMessageConsumer StartSubscriptionClient(string[] topics, string subscriptionName, string consumerId, OnMessagesReceived onMessagesReceived, ConsumerConfig consumerConfig = null)
        {
            return null;
        }

        public IMessageConsumer StartSubscriptionClient(string topic, string subscriptionName, string consumerId,
                                                         OnMessagesReceived onMessagesReceived, ConsumerConfig consumerConfig = null)
        {
            return null;
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null,
                                           string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            return new EmptyMessageContext();
        }
    }
}