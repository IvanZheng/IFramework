﻿using System.Text;
using Confluent.Kafka;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.Extensions.Options;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaMQClientProvider : IMessageQueueClientProvider
    {
        private readonly string _brokerList;

        public KafkaMQClientProvider(IOptions<KafkaClientOptions> options)
        {
            _brokerList = options.Value.BrokerList;
        }

        public IMessageProducer CreateQueueProducer(string queue, ProducerConfig config = null)
        {
            return new KafkaProducer(queue, _brokerList, config);
        }

        public IMessageProducer CreateTopicProducer(string topic, ProducerConfig config = null)
        {
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
                                                    ConsumerConfig consumerConfig,
                                                    bool start = true)
        {
            var consumer = new KafkaConsumer<string, KafkaMessage>(_brokerList, new []{queue}, $"{queue}.consumer", consumerId,
                                                                   BuildOnKafkaMessageReceived(onMessagesReceived),
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
                                                        bool start = true)
        {
            var consumer = new KafkaConsumer<string, KafkaMessage>(_brokerList, topics, subscriptionName, consumerId,
                                                                   BuildOnKafkaMessageReceived(onMessagesReceived),
                                                                   consumerConfig);
            if (start)
            {
                consumer.Start();
            }
            return consumer;
        }

        private OnKafkaMessageReceived<string, KafkaMessage> BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message, cancellationToken) =>
            {
                var kafkaMessage = message.Message;
                var messageContext = new MessageContext(kafkaMessage.Value, message.Topic, message.Partition, message.Offset);
                onMessagesReceived(cancellationToken, messageContext);
            };
        }

        public void Dispose() { }
    }
}