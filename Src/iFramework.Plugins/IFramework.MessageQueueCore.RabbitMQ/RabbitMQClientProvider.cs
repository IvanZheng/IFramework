using System;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using RabbitMQ.Client;

namespace IFramework.MessageQueueCore.RabbitMQ
{
    public class RabbitMQClientProvider : IMessageQueueClientProvider
    {
        private readonly IConnection _connection;
        public RabbitMQClientProvider(string broker)
        {
            var factory = new ConnectionFactory {Uri = new Uri(broker)};
            //"amqp://user:pass@hostName:port/vhost";
            _connection = factory.CreateConnection();

        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageConsumer CreateTopicSubscription(string topic,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateTopicProducer(string topic)
        {
            throw new NotImplementedException();
        }

        public IMessageProducer CreateQueueProducer(string queue)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}