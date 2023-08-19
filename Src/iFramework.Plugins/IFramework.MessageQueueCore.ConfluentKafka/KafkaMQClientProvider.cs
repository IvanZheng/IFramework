using System;
using System.Collections.Generic;
using Confluent.Kafka;
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
                                       DefaultBuildMessageContext(),
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
                                           DefaultBuildMessageContext(),
                                           consumerId,
                                           config,
                                           start);
        }


        public void Dispose() { }

        public IMessageConsumer CreateQueueConsumer<TKafkaMessage>(string queue,
                                                                   OnMessagesReceived onMessagesReceived,
                                                                   Func<ConsumeResult<string, TKafkaMessage>, IMessageContext> buildMessageContext,
                                                                   string consumerId,
                                                                   ConsumerConfig config,
                                                                   bool start = true)
        {
            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);
            var consumer = new KafkaConsumer<string, TKafkaMessage>(_brokerList,
                                                                    new[] { queue },
                                                                    $"{queue}{FrameworkConfigurationExtension.QueueNameSplit}consumer", consumerId,
                                                                    BuildOnKafkaMessageReceived(onMessagesReceived, buildMessageContext),
                                                                    config);
            if (start)
            {
                consumer.Start();
            }

            return consumer;
        }


        public IMessageConsumer CreateTopicSubscription<TKafkaMessage>(string[] topics,
                                                                       string subscriptionName,
                                                                       OnMessagesReceived onMessagesReceived,
                                                                       Func<ConsumeResult<string, TKafkaMessage>, IMessageContext> buildMessageContext,
                                                                       string consumerId,
                                                                       ConsumerConfig config,
                                                                       bool start = true)
        {
            config = config ?? new ConsumerConfig();
            FillExtensions(config.Extensions);

            var consumer = new KafkaConsumer<string, TKafkaMessage>(_brokerList,
                                                                    topics,
                                                                    subscriptionName,
                                                                    consumerId,
                                                                    BuildOnKafkaMessageReceived(onMessagesReceived, buildMessageContext),
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

        private static Func<ConsumeResult<string, KafkaMessage>, IMessageContext> DefaultBuildMessageContext()
        {
            return message =>
            {
                var kafkaMessage = message.Message;
                return new MessageContext(kafkaMessage.Value,
                                          message.Topic,
                                          message.Partition,
                                          message.Offset);
            };
        }

        private static OnKafkaMessageReceived<string, TKafkaMessage> BuildOnKafkaMessageReceived<TKafkaMessage>(OnMessagesReceived onMessagesReceived,
                                                                                                                Func<ConsumeResult<string, TKafkaMessage>, IMessageContext> buildMessageContext)
        {
            return (consumer, message, cancellationToken) =>
            {
                var messageContext = buildMessageContext(message);
                onMessagesReceived(cancellationToken, messageContext);
            };
        }
    }
}