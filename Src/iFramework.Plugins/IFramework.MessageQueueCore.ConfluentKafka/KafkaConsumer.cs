﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.Infrastructure;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public delegate void OnKafkaMessageReceived<TKey, TValue>(KafkaConsumer<TKey, TValue> consumer, ConsumeResult<TKey, TValue> message, CancellationToken cancellationToken);

    public class KafkaConsumer<TKey, TValue> : MessageConsumer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        protected readonly string BrokerList;
        private IConsumer<TKey, TValue> _consumer;
        protected OnKafkaMessageReceived<TKey, TValue> OnMessageReceived;

        public KafkaConsumer(string brokerList,
                             string[] topics,
                             string groupId,
                             string consumerId,
                             OnKafkaMessageReceived<TKey, TValue> onMessageReceived,
                             ConsumerConfig consumerConfig = null)
            : base(topics, groupId, consumerId, consumerConfig)
        {
            BrokerList = brokerList;
            if (string.IsNullOrWhiteSpace(brokerList))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(brokerList));
            }

            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupId));
            }

            OnMessageReceived = onMessageReceived;

            ConsumerConfiguration = new Confluent.Kafka.ConsumerConfig
            {
                GroupId = GroupId,
                ClientId = consumerId,
                EnableAutoCommit = false,
                //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                SocketNagleDisable = true,
                //{"statistics.interval.ms", 60000},
                //{"retry.backoff.ms", ConsumerConfig.BackOffIncrement.ToString()},
                BootstrapServers = BrokerList,
                AutoOffsetReset = (Confluent.Kafka.AutoOffsetReset) ConsumerConfig.AutoOffsetReset
            };
        }

        public Confluent.Kafka.ConsumerConfig ConsumerConfiguration { get; protected set; }

        protected override void PollMessages(CancellationToken cancellationToken)
        {
            //_consumer.Poll(TimeSpan.FromMilliseconds(1000));
            var consumeResult = _consumer.Consume(cancellationToken);
            if (consumeResult != null)
            {
                _consumer_OnMessage(_consumer, consumeResult, cancellationToken);
            }
        }

        public override void Start()
        {
            _consumer = new ConsumerBuilder<TKey, TValue>(ConsumerConfiguration).SetValueDeserializer(new KafkaMessageDeserializer<TValue>())
                                                                                .Build();
            _consumer.Subscribe(Topics);
            //_consumer.OnError += (sender, error) => Logger.LogError($"consumer({Id}) error: {error.ToJson()}");
            base.Start();
        }


        private void _consumer_OnMessage(object sender, ConsumeResult<TKey, TValue> message, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug($"consume message: {message.Topic}.{message.Partition}.{message.Offset}");
                AddMessageOffset(message.Topic, message.Partition, message.Offset);
                OnMessageReceived(this, message, cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{0} topic:{1} partition:{2} offset:{3} _consumer_OnMessage failed!",
                                Id, message.Topic, message.Partition, message.Offset);
                if (message.Value != null)
                {
                    FinishConsumingMessage(new MessageOffset(null, message.Topic, message.Partition, message.Offset));
                }
            }
        }

        public override Task CommitOffsetAsync(string broker, string topic, int partition, long offset)
        {
            // kafka not use broker in cluster mode
            var topicPartitionOffset = new TopicPartitionOffset(new TopicPartition(topic, partition), offset + 1);
            _consumer.Commit(new[] {topicPartitionOffset});
            return Task.CompletedTask;
        }

        public override void Stop()
        {
            _cancellationTokenSource.Cancel(true);
            base.Stop();
            _consumer?.Dispose();
        }
    }
}