using System;
using System.Threading;
using System.Threading.Tasks;
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

        public ICommitOffsetable StartQueueClient(string commandQueueName,
                                                  string consumerId,
                                                  OnMessagesReceived onMessagesReceived,
                                                  ConsumerConfig consumerConfig = null)
        {
            throw new NotImplementedException();
        }

        public ICommitOffsetable StartSubscriptionClient(string topic,
                                                         string subscriptionName,
                                                         string consumerId,
                                                         OnMessagesReceived onMessagesReceived,
                                                         ConsumerConfig consumerConfig = null)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(IMessageContext messageContext, string queue, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(IMessageContext messageContext, string topic, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}