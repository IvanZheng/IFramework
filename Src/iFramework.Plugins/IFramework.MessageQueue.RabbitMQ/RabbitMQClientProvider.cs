using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.RabbitMQ.MessageFormat;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class RabbitMQClientProvider : IMessageQueueClientProvider
    {
        private readonly IConnection _connection;

        public RabbitMQClientProvider(string hostName)
        {
            _connection = new ConnectionFactory {HostName = hostName}.CreateConnection();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            var messageContext = new MessageContext(message, messageId)
            {
                Producer = producer,
                Ip = Utility.GetLocalIpv4()?.ToString()
            };
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationId = correlationId;
            }
            if (!string.IsNullOrEmpty(topic))
            {
                messageContext.Topic = topic;
            }
            if (!string.IsNullOrEmpty(key))
            {
                messageContext.Key = key;
            }
            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                messageContext.SagaInfo = sagaInfo;
            }
            return messageContext;
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(commandQueueName, true, false, false, null);

            var consumer = new RabbitMQConsumer(channel,
                                        commandQueueName,
                                        $"{commandQueueName}.consumer",
                                        consumerId,
                                        BuildOnRabbitMQMessageReceived(onMessagesReceived),
                                        consumerConfig);
            if (start)
            {
                consumer.Start();
            }
            return consumer;
        }

        public IMessageConsumer CreateTopicSubscription(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig, bool start = true)
        {
            var channel = _connection.CreateModel();
            channel.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);
            var queueName = channel.QueueDeclare(subscriptionName, true, false, false, null).QueueName;
            channel.QueueBind(queueName,
                              topic,
                              string.Empty);
            var subscriber = new RabbitMQConsumer(channel,
                                        topic,
                                        subscriptionName,
                                        consumerId,
                                        BuildOnRabbitMQMessageReceived(onMessagesReceived),
                                        consumerConfig);
            if (start)
            {
                subscriber.Start();
            }

            return subscriber;
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            var channel = _connection.CreateModel();
            channel.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);

            return new RabbitMQProducer(channel, topic, topic, config);
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(queue, true, false, false, null);

            return new RabbitMQProducer(channel, null, queue, config);
        }

        private OnRabbitMQMessageReceived BuildOnRabbitMQMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, args) =>
            {
                var message = Encoding.UTF8.GetString(args.Body).ToJsonObject<RabbitMQMessage>();
                var messageContext = new MessageContext(message, new MessageOffset(string.Empty, 0, (long) args.DeliveryTag));
                onMessagesReceived(messageContext);
            };
        }
    }
}