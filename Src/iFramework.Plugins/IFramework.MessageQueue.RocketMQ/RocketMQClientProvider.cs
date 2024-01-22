using System;
using System.Collections.Generic;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.RocketMQ
{
    public class RocketMQClientProvider : IMessageQueueClientProvider
    {
        private readonly IMessageContextBuilder _defaultMessageContextBuilder = new DefaultRocketMQMessageContextBuilder();
        private readonly string _endpoints;
        private readonly RocketMQClientOptions _options;
        private readonly IMessageTypeProvider _messageTypeProvider;
        public RocketMQClientProvider(IOptions<RocketMQClientOptions> options, IMessageTypeProvider messageTypeProvider)
        {
            _endpoints = options.Value.Endpoints;
            _options = options.Value;
            _messageTypeProvider = messageTypeProvider;
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            config ??= new ProducerConfig();
            FillExtensions(config.Extensions);
            return new RocketMQProducer(_endpoints, new[] { queue }, config);
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            config ??= new ProducerConfig();
            FillExtensions(config.Extensions);
            return new RocketMQProducer(_endpoints, new[] { topic }, config);
        }


        public void Dispose() { }

        public IMessageConsumer CreateQueueConsumer(string queue,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig config,
                                                    bool start = true,
                                                    IMessageContextBuilder messageContextBuilder = null)
        {
            if (messageContextBuilder == null)
            {
                messageContextBuilder = _defaultMessageContextBuilder;
            }

            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);
            var consumer = new RocketMQConsumer(_endpoints,
                                                new[] { queue },
                                                $"{queue}{FrameworkConfigurationExtension.QueueNameSplit}consumer", consumerId,
                                                BuildOnRocketMQMessageReceived(onMessagesReceived,
                                                                               messageContextBuilder as IRocketMQMessageContextBuilder),
                                                config);
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
                                                        ConsumerConfig config,
                                                        bool start = true,
                                                        IMessageContextBuilder messageContextBuilder = null)
        {
            if (messageContextBuilder == null)
            {
                messageContextBuilder = _defaultMessageContextBuilder;
            }

            config ??= new ConsumerConfig();
            FillExtensions(config.Extensions);

            var consumer = new RocketMQConsumer(_endpoints,
                                                topics,
                                                subscriptionName,
                                                consumerId,
                                                BuildOnRocketMQMessageReceived(onMessagesReceived, messageContextBuilder as IRocketMQMessageContextBuilder),
                                                config);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }

        private void FillExtensions(Dictionary<string, object> extensions)
        {
            if (_options.Extensions?.Count > 0)
            {
                _options.Extensions.ForEach(p => extensions[p.Key] = p.Value);
            }
        }


        private OnRocketMQMessageReceived BuildOnRocketMQMessageReceived(OnMessagesReceived onMessagesReceived,
                                                                         IRocketMQMessageContextBuilder messageContextBuilder)
        {
            if (messageContextBuilder == null)
            {
                throw new ArgumentNullException(nameof(messageContextBuilder), "please use valid IKafkaMessageContextBuilder<TKafkaMessage>");
            }

            return (consumer, message, cancellationToken) =>
            {
                var messageContext = messageContextBuilder.Build(message, _messageTypeProvider);
                onMessagesReceived(cancellationToken, messageContext);
            };
        }
    }
}