using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public delegate void OnKafkaMessageReceived<TKey, TValue>(KafkaConsumer<TKey, TValue> consumer, ConsumeResult<TKey, TValue> message);

    public class KafkaConsumer<TKey, TValue> : MessageConsumer
    {
        private readonly IDeserializer<TKey> _keyDeserializer;
        private readonly IDeserializer<TValue> _valueDeserializer;
        private Consumer<TKey, TValue> _consumer;
        protected OnKafkaMessageReceived<TKey, TValue> OnMessageReceived;
        protected readonly string BrokerList;
        public KafkaConsumer(string brokerList,
                             string topic,
                             string groupId,
                             string consumerId,
                             OnKafkaMessageReceived<TKey, TValue> onMessageReceived,
                             IDeserializer<TKey> keyDeserializer,
                             IDeserializer<TValue> valueDeserializer,
                             ConsumerConfig consumerConfig = null)
            : base(topic, groupId, consumerId, consumerConfig)
        {
            _keyDeserializer = keyDeserializer ?? throw new ArgumentNullException(nameof(keyDeserializer));
            _valueDeserializer = valueDeserializer ?? throw new ArgumentNullException(nameof(valueDeserializer));
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

            ConsumerConfiguration = new Dictionary<string, object>
            {
                {"group.id", GroupId},
                {"client.id", consumerId},
                {"enable.auto.commit", false},
                //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                {"socket.nagle.disable", true},
                //{"statistics.interval.ms", 60000},
                {"retry.backoff.ms", ConsumerConfig.BackOffIncrement},
                {"bootstrap.servers", BrokerList},
                {"auto.offset.reset", ConsumerConfig.AutoOffsetReset}
            };
        }

        public Dictionary<string, object> ConsumerConfiguration { get; protected set; }
        
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        protected override void PollMessages()
        {
            var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
            if (consumeResult != null)
            {
                _consumer_OnMessage(consumeResult);
            }
        }

        public override void Start()
        {
            _consumer = new Consumer<TKey, TValue>(ConsumerConfiguration, _keyDeserializer, _valueDeserializer);
            _consumer.Subscribe(Topic);
            _consumer.OnError += (sender, error) => Logger.LogError($"consumer({Id}) error: {error.ToJson()}");
            base.Start();
        }


        private void _consumer_OnMessage(ConsumeResult<TKey, TValue> message)
        {
            try
            {
                AddMessageOffset(message.Partition, message.Offset);
                OnMessageReceived(this, message);
            }
            catch (OperationCanceledException) { }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                if (message.Value != null)
                {
                    FinishConsumingMessage(new MessageOffset(null, message.Partition, message.Offset));
                }
                Logger.LogError(ex, $"{Id} _consumer_OnMessage failed!");
            }
        }

        public override Task CommitOffsetAsync(string broker, int partition, long offset)
        {
            // kafka not use broker in cluster mode
            var topicPartitionOffset = new TopicPartitionOffset(new TopicPartition(Topic, partition), offset + 1);
            return _consumer.CommitAsync(new[] {topicPartitionOffset}, _cancellationTokenSource.Token);
        }

        public override void Stop()
        {
            _cancellationTokenSource.Cancel(true);
            base.Stop();
            _consumer?.Dispose();
        }
    }
}