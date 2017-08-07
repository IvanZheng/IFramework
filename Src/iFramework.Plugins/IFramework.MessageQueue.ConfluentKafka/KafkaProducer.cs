using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaProducer<TKey, TValue>
    {
        private readonly ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaProducer<TKey, TValue>).Name);
        private readonly Producer<TKey, TValue> _producer;
        private readonly string _topic;
        private readonly string _brokerList;
        private readonly ISerializer<TKey> _keySerializer;
        private readonly ISerializer<TValue> _valueSerializer;

        public KafkaProducer(string topic, string brokerList, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            _keySerializer = keySerializer ?? throw new ArgumentNullException(nameof(keySerializer));
            _valueSerializer = valueSerializer ?? throw new ArgumentNullException(nameof(valueSerializer));
            _brokerList = brokerList;
            _topic = topic;
            var producerConfiguration = new Dictionary<string, object>
            {
                { "bootstrap.servers", _brokerList },
                {"request.required.acks", 1},
                {"socket.nagle.disable", true},
                {"socket.blocking.max.ms", 10},
                {"queue.buffering.max.ms", 10}
            };

            _producer = new Producer<TKey, TValue>(producerConfiguration, _keySerializer, valueSerializer);
            _producer.OnError += _producer_OnError;
        }

        private void _producer_OnError(object sender, Error e)
        {
            _logger.Error($"producer topic({_topic}) error: {e.ToJson()}");
        }

        public void Stop()
        {
            try
            {
                _producer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error($"{_topic} producer dispose failed", ex);
            }
        }

        //public Message<string, KafkaMessage> Send(string key, KafkaMessage message)
        //{
        //    var result = _producer.ProduceAsync(_topic, key, message).Result;
        //    //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
        //    return result;
        //}

        public async Task<Message<TKey, TValue>> SendAsync(TKey key, TValue message, CancellationToken cancellationToken)
        {
            var retryTimes = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    var result = await _producer.ProduceAsync(_topic, key, message, false)
                                                .ConfigureAwait(false);
                    if (result.Error != ErrorCode.NoError)
                    {
                        _logger.Error($"send message failed topic: {_topic} Partition: {result.Partition} key:{key} error:{result.Error}");
                        await Task.Delay(waitTime, cancellationToken);
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"send message failed topic: {_topic} key:{key}", e);
                    await Task.Delay(waitTime, cancellationToken);
                }

            }
            //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
        }
    }
}