using System;
using System.Collections.Generic;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaMQClientProvider : IMessageQueueClientProvider
    {
        private readonly string _brokerList;
        private readonly IMessageContextBuilder _defaultMessageContextBuilder = new DefaultKafkaMessageContextBuilder();
        private readonly KafkaClientOptions _options;

        public KafkaMQClientProvider(IOptions<KafkaClientOptions> options)
        {
            _brokerList = options.Value.BrokerList;
            _options = options.Value;
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            config = config ?? new ProducerConfig();
            FillExtensions(config.Extensions);
            return new KafkaProducer(queue, _brokerList, config);
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
            config = config ?? new ProducerConfig();
            FillExtensions(config.Extensions);
            return new KafkaProducer(topic, _brokerList, config);
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
            var consumer = new KafkaConsumer(_brokerList,
                                             new[] { queue },
                                             $"{queue}{FrameworkConfigurationExtension.QueueNameSplit}consumer", consumerId,
                                             BuildOnKafkaMessageReceived(onMessagesReceived,
                                                                         messageContextBuilder as IKafkaMessageContextBuilder),
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

            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);

            var consumer = new KafkaConsumer(_brokerList,
                                             topics,
                                             subscriptionName,
                                             consumerId,
                                             BuildOnKafkaMessageReceived(onMessagesReceived, messageContextBuilder as IKafkaMessageContextBuilder),
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


        private static OnKafkaMessageReceived BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived,
                                                                          IKafkaMessageContextBuilder messageContextBuilder)
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