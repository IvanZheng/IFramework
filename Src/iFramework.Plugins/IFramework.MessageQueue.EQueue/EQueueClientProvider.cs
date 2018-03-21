using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.EQueue.MessageFormat;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueClientProvider : IMessageQueueClientProvider
    {
        private OnEQueueMessageReceived BuildOnEQueueMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message) =>
            {
                var equeueMessage = Encoding.UTF8.GetString(message.Body).ToJsonObject<EQueueMessage>();
                var messageContext = new MessageContext(equeueMessage, message.BrokerName, message.QueueId, message.QueueOffset);
                onMessagesReceived(messageContext);
            };
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateQueueProducer(string queue)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateTopicProducer(string topic)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateTopicSubscription(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            throw new NotImplementedException();
        }
    }
}
