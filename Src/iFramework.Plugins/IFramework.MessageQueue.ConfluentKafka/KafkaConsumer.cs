using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public delegate void OnKafkaMessageReceived<TKey, TValue>(KafkaConsumer<TKey, TValue> consumer, Message<TKey, TValue> message);

    public class KafkaConsumer<TKey, TValue> : ICommitOffsetable
    {
        protected CancellationTokenSource _cancellationTokenSource;
        private Consumer<TKey, TValue> _consumer;
        protected Task _consumerTask;
        protected ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaConsumer<TKey, TValue>).Name);
        protected OnKafkaMessageReceived<TKey, TValue> _onMessageReceived;
        protected ConsumerConfig _consumerConfig;
        private readonly IDeserializer<TKey> _keyDeserializer;
        private readonly IDeserializer<TValue> _valueDeserializer;
        public KafkaConsumer(string brokerList,
                             string topic,
                             string groupId,
                             string consumerId,
                             OnKafkaMessageReceived<TKey, TValue> onMessageReceived,
                             IDeserializer<TKey> keyDeserializer,
                             IDeserializer<TValue> valueDeserializer,
                             ConsumerConfig consumerConfig = null,
                             bool start = true)
        {
            _keyDeserializer = keyDeserializer ?? throw new ArgumentNullException(nameof(keyDeserializer));
            _valueDeserializer = valueDeserializer ?? throw new ArgumentNullException(nameof(valueDeserializer));

            if (string.IsNullOrWhiteSpace(brokerList))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(brokerList));
            }
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupId));
            }
            _consumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            BrokerList = brokerList;
            Topic = topic;
            GroupId = groupId;
            ConsumerId = consumerId ?? string.Empty;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
            ConsumerConfiguration = new Dictionary<string, object>
            {
                {"group.id", GroupId},
                {"client.id", consumerId},
                {"enable.auto.commit", false},
                {"socket.blocking.max.ms", 10},
                {"fetch.error.backoff.ms", 10 },
                {"socket.nagle.disable", true},
                //{"statistics.interval.ms", 60000},
                {"retry.backoff.ms", _consumerConfig.BackOffIncrement},
                {"bootstrap.servers", BrokerList},
                {
                    "default.topic.config", new Dictionary<string, object>
                    {
                        {"auto.offset.reset", _consumerConfig.AutoOffsetReset}
                    }
                }
            };
            _onMessageReceived = onMessageReceived;
            if (start)
            {
                Start();
            }
        }

        public string BrokerList { get; protected set; }
        public string Topic { get; protected set; }
        public string GroupId { get; protected set; }
        public string ConsumerId { get; protected set; }
        public Dictionary<string, object> ConsumerConfiguration { get; protected set; }
        public ConcurrentDictionary<int, SlidingDoor> SlidingDoors { get; protected set; }

        public string Id => $"{GroupId}.{Topic}.{ConsumerId}";

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _consumer = new Consumer<TKey, TValue>(ConsumerConfiguration, _keyDeserializer, _valueDeserializer);//new StringDeserializer(Encoding.UTF8), new KafkaMessageDeserializer());
            _consumer.Subscribe(Topic);
            _consumer.OnError += (sender, error) => _logger.Error($"consumer({Id}) error: {error.ToJson()}");
            _consumer.OnMessage += _consumer_OnMessage;

            _consumerTask = Task.Factory.StartNew(cs => ReceiveMessages(cs as CancellationTokenSource),
                                                  _cancellationTokenSource,
                                                  _cancellationTokenSource.Token,
                                                  TaskCreationOptions.LongRunning,
                                                  TaskScheduler.Default);
        }



        private void _consumer_OnMessage(object sender, Message<TKey, TValue> message)
        {
            try
            {
                AddMessage(message);
                _onMessageReceived(this, message);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (message.Value != null)
                {
                    RemoveMessage(message.Partition, message.Offset);
                }
                _logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            var message = messageContext as MessageContext;
            RemoveMessage(message.Partition, message.Offset);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(true);
            _consumerTask?.Wait();
            _consumerTask?.Dispose();
            _consumer?.Dispose();
            _consumerTask = null;
            _cancellationTokenSource = null;
            SlidingDoors.Clear();
        }

        protected void ReStart()
        {
            Stop();
            Start();
        }

        private void ReceiveMessages(CancellationTokenSource cancellationTokenSource)
        {
            #region peek messages that not been consumed since last time
            Console.WriteLine($"ReceiveMessages start");
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    //var linkedTimeoutCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token,
                    //                                                                       new CancellationTokenSource(3000).Token);
                    _consumer.Poll(100);
                    BlockIfFullLoad();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Task.Delay(1000).Wait();
                        _logger.Error(ex.GetBaseException().Message, ex);
                    }
                }
            }

            #endregion
        }


        protected void BlockIfFullLoad()
        {
            while (SlidingDoors.Sum(d => d.Value.MessageCount) > _consumerConfig.FullLoadThreshold)
            {
                Task.Delay(_consumerConfig.WaitInterval).Wait();
                _logger.Warn($"working is full load sleep 1000 ms");
            }
        }

        protected void AddMessage(Message<TKey, TValue> message)
        {
            var slidingDoor = SlidingDoors.GetOrAdd(message.Partition, partition => new SlidingDoor(CommitOffset,
                                                                                                    string.Empty,
                                                                                                    partition,
                                                                                                    Configuration.Instance.GetCommitPerMessage()));
            slidingDoor.AddOffset(message.Offset);
        }

        internal void RemoveMessage(int partition, long offset)
        {
            var slidingDoor = SlidingDoors.TryGetValue(partition);
            if (slidingDoor == null)
            {
                throw new Exception("partition slidingDoor not exists");
            }
            slidingDoor.RemoveOffset(offset);
        }



        public async Task CommitOffsetAsync(int partition, long offset)
        {
            // kafka not use broker in cluster mode
            var topicPartitionOffset = new TopicPartitionOffset(new TopicPartition(Topic, partition), offset + 1);
            var committedOffset = await _consumer.CommitAsync(new[] { topicPartitionOffset })
                                                 .ConfigureAwait(false);
            if (committedOffset.Error.Code != ErrorCode.NoError)
            {
                _logger.Error($"{Id} committed offset failed {committedOffset.Error}");
            }
            else
            {
                _logger.DebugFormat($"{Id} committed offset {committedOffset.Offsets.FirstOrDefault()}");
            }
        }

        public void CommitOffset(int partition, long offset)
        {
            // kafka not use broker in cluster mode
            CommitOffset(null, partition, offset);
        }

        private void CommitOffset(string broker, int partition, long offset)
        {
            // kafka not use broker in cluster mode
            CommitOffsetAsync(partition, offset);
        }
    }
}