using System;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.MessageQueue.Client.Abstracts;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class RabbitMQClientProvider : IMessageQueueClientProvider
    {
        private readonly IConnection _connection;
        private readonly IMessageContextBuilder _defaultMessageContextBuilder = new DefaultRabbitMQMessageContextBuilder();


        public RabbitMQClientProvider(ConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            _connection = connectionFactory.CreateConnection();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true,
                                                    IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;

            var channel = _connection.CreateModel();

            channel.QueueDeclare(commandQueueName, true, false, false, null);
            channel.BasicQos(0, (ushort)consumerConfig.FullLoadThreshold, false);
            var consumer = new RabbitMQConsumer(channel,
                                                new[] { commandQueueName },
                                                commandQueueName,
                                                consumerId,
                                                BuildOnKafkaMessageReceived(onMessagesReceived, messageContextBuilder as IRabbitMQMessageContextBuilder),
                                                consumerConfig);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }

        public IMessageConsumer CreateTopicSubscription(string[] topics,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true,
                                                        IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;
            var channel = _connection.CreateModel();
            var queueName = channel.QueueDeclare(subscriptionName, true, false, false, null).QueueName;

            topics.ForEach(topic =>
            {
                channel.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);
                channel.QueueBind(queueName,
                                  topic,
                                  string.Empty);
            });

            var subscriber = new RabbitMQConsumer(channel,
                                                  topics,
                                                  queueName,
                                                  consumerId,
                                                  BuildOnKafkaMessageReceived(onMessagesReceived, messageContextBuilder as IRabbitMQMessageContextBuilder),
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
            topic = Configuration.Instance.FormatMessageQueueName(topic);

            channel.ExchangeDeclare(topic, ExchangeType.Fanout, true, false, null);

            return new RabbitMQProducer(channel, topic, string.Empty, config);
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            var channel = _connection.CreateModel();
            queue = Configuration.Instance.FormatMessageQueueName(queue);

            channel.QueueDeclare(queue, true, false, false, null);

            return new RabbitMQProducer(channel, string.Empty, queue, config);
        }

        private static OnRabbitMQMessageReceived BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived,
                                                                             IRabbitMQMessageContextBuilder messageContextBuilder)
        {
            if (messageContextBuilder == null)
            {
                throw new ArgumentNullException(nameof(messageContextBuilder), "please use valid IKafkaMessageContextBuilder<TKafkaMessage>");
            }

            return (consumer, message, cancellationToken) =>
            {
                var messageContext = messageContextBuilder.Build(message);
                onMessagesReceived(cancellationToken, messageContext);
            };
        }
    }
}