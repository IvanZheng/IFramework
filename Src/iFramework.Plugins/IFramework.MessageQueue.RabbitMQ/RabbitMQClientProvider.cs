using System;
using System.Threading.Tasks;
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

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public async Task<IMessageConsumer> CreateQueueConsumerAsync(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true,
                                                    IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;

            var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(commandQueueName, true, false, false, null);
            await channel.BasicQosAsync(0, (ushort)consumerConfig.FullLoadThreshold, false);
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

        public IMessageConsumer CreateQueueConsumer(string commandQueueName,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true,
                                                    IMessageContextBuilder messageContextBuilder = null)
        {
            return CreateQueueConsumerAsync(commandQueueName, onMessagesReceived, consumerId, consumerConfig, start, messageContextBuilder).GetAwaiter().GetResult();
        }

        public async Task<IMessageConsumer> CreateTopicSubscriptionAsync(string[] topics,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true,
                                                        IMessageContextBuilder messageContextBuilder = null)
        {
            messageContextBuilder ??= _defaultMessageContextBuilder;
            var channel = await _connection.CreateChannelAsync();
            var queueDeclareOk = await channel.QueueDeclareAsync(subscriptionName, true, false, false, null);
            var queueName = queueDeclareOk.QueueName;

            foreach (var topic in topics)
            {
                await channel.ExchangeDeclareAsync(topic, ExchangeType.Fanout, true, false, null);
                await channel.QueueBindAsync(queueName,
                                  topic,
                                  string.Empty);
            }

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

        public IMessageConsumer CreateTopicSubscription(string[] topics,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig consumerConfig,
                                                        bool start = true,
                                                        IMessageContextBuilder messageContextBuilder = null)
        {
            return CreateTopicSubscriptionAsync(topics, subscriptionName, onMessagesReceived, consumerId, consumerConfig, start, messageContextBuilder).GetAwaiter().GetResult();
        }

        public async Task<IMessageProducer> CreateTopicProducerAsync(string topic, ProducerConfig config = null)
        {
            var channel = await _connection.CreateChannelAsync();
            topic = Configuration.Instance.FormatMessageQueueName(topic);

            await channel.ExchangeDeclareAsync(topic, ExchangeType.Fanout, true, false, null);

            return new RabbitMQProducer(channel, topic, string.Empty, config);
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            return CreateTopicProducerAsync(topic, config).GetAwaiter().GetResult();
        }

        public async Task<IMessageProducer> CreateQueueProducerAsync(string queue, ProducerConfig config = null)
        {
            var channel = await _connection.CreateChannelAsync();
            queue = Configuration.Instance.FormatMessageQueueName(queue);

            await channel.QueueDeclareAsync(queue, true, false, false, null);

            return new RabbitMQProducer(channel, string.Empty, queue, config);
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            return CreateQueueProducerAsync(queue, config).GetAwaiter().GetResult();
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