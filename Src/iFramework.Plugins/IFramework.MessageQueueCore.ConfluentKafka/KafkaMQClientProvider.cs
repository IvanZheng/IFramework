using System;
using System.Collections.Generic;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaMQClientProvider : IMessageQueueClientProvider
    {
        private readonly string _brokerList;
        private readonly IMessageContextBuilder<PayloadMessage> _defaultMessageContextBuilder = new DefaultKafkaMessageContextBuilder();
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

        public IMessageConsumer CreateQueueConsumer(string queue,
                                                    OnMessagesReceived onMessagesReceived,
                                                    string consumerId,
                                                    ConsumerConfig config,
                                                    bool start = true)
        {
            return CreateQueueConsumer(queue,
                                       onMessagesReceived,
                                       _defaultMessageContextBuilder,
                                       consumerId,
                                       config,
                                       start);
        }

        public IMessageConsumer CreateTopicSubscription(string[] topics,
                                                        string subscriptionName,
                                                        OnMessagesReceived onMessagesReceived,
                                                        string consumerId,
                                                        ConsumerConfig config,
                                                        bool start = true)
        {
            return CreateTopicSubscription(topics,
                                           subscriptionName,
                                           onMessagesReceived,
                                           _defaultMessageContextBuilder,
                                           consumerId,
                                           config,
                                           start);
        }


        public void Dispose() { }

        public IMessageConsumer CreateQueueConsumer<TPayloadMessage>(string queue,
                                                                     OnMessagesReceived onMessagesReceived,
                                                                     IMessageContextBuilder<TPayloadMessage> messageContextBuilder,
                                                                     string consumerId,
                                                                     ConsumerConfig config,
                                                                     bool start = true)
        {
            if (typeof(TPayloadMessage) == typeof(PayloadMessage) && messageContextBuilder == null)
            {
                messageContextBuilder = _defaultMessageContextBuilder as IMessageContextBuilder<TPayloadMessage>;
            }
            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);
            var consumer = new KafkaConsumer<string, TPayloadMessage>(_brokerList,
                                                                      new[] { queue },
                                                                      $"{queue}{FrameworkConfigurationExtension.QueueNameSplit}consumer", consumerId,
                                                                      BuildOnKafkaMessageReceived(onMessagesReceived,
                                                                                                  messageContextBuilder as IKafkaMessageContextBuilder<TPayloadMessage>),
                                                                      config);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }


        public IMessageConsumer CreateTopicSubscription<TPayloadMessage>(string[] topics,
                                                                         string subscriptionName,
                                                                         OnMessagesReceived onMessagesReceived,
                                                                         IMessageContextBuilder<TPayloadMessage> messageContextBuilder,
                                                                         string consumerId,
                                                                         ConsumerConfig config,
                                                                         bool start = true)
        {
            if (typeof(TPayloadMessage) == typeof(PayloadMessage) && messageContextBuilder == null)
            {
                messageContextBuilder = _defaultMessageContextBuilder as IMessageContextBuilder<TPayloadMessage>;
            }
            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);

            var consumer = new KafkaConsumer<string, TPayloadMessage>(_brokerList,
                                                                      topics,
                                                                      subscriptionName,
                                                                      consumerId,
                                                                      BuildOnKafkaMessageReceived(onMessagesReceived, messageContextBuilder as IKafkaMessageContextBuilder<TPayloadMessage>),
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


        private static OnKafkaMessageReceived<string, TKafkaMessage> BuildOnKafkaMessageReceived<TKafkaMessage>(OnMessagesReceived onMessagesReceived,
                                                                                                                IKafkaMessageContextBuilder<TKafkaMessage> messageContextBuilder)
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