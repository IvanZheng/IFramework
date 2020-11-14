using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaProducer : KafkaProducer<string, KafkaMessage>, IMessageProducer
    {
        public KafkaProducer(string topic,
                             string brokerList,
                             ProducerConfig config = null)
            : base(topic, brokerList, config) { }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var message = ((MessageContext) messageContext).KafkaMessage;
            var topic = Configuration.Instance.FormatMessageQueueName(messageContext.Topic);
            return SendAsync(topic, messageContext.Key ?? messageContext.MessageId, message, cancellationToken);
        }
    }

    public class KafkaProducer<TKey, TValue>
    {
        public ProducerConfig Config { get; private set; }
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(typeof(KafkaProducer<TKey, TValue>));
        private readonly IProducer<TKey, TValue> _producer;
        private readonly string _topic;

        public KafkaProducer(string topic,
                             string brokerList,
                             ProducerConfig config = null)
        {
            Config = config ??  new ProducerConfig();
            
            _topic = topic;
            var producerConfiguration = new Confluent.Kafka.ProducerConfig
            {
                BootstrapServers = brokerList,
                Acks = (Acks)(Config["request.required.acks"] ?? 1),
                SocketNagleDisable = true
                //{"socket.blocking.max.ms", Config["socket.blocking.max.ms"] ?? 50},
                //{"queue.buffering.max.ms", Config["queue.buffering.max.ms"] ?? 50}
            };

            _producer = new ProducerBuilder<TKey, TValue>(producerConfiguration).SetValueSerializer(new KafkaMessageSerializer<TValue>())
                                                                                .Build();
            //_producer.OnError += _producer_OnError;
        }

        private void _producer_OnError(object sender, Error e)
        {
            _logger.LogError($"producer topic({_topic}) error: {e.ToJson()}");
        }

        public void Stop()
        {
            try
            {
                _producer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{_topic} producer dispose failed", ex);
            }
        }

        public async Task<DeliveryResult<TKey, TValue>> SendAsync(string topic, TKey key, TValue message, CancellationToken cancellationToken)
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
                    var result = await _producer.ProduceAsync(topic,
                                                              new Message<TKey, TValue>{
                                                                  Key = key,
                                                                  Value = message
                                                              }, cancellationToken)
                                                .ConfigureAwait(false);
                    return result;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"send message failed topic: {topic} key:{key}");
                    await Task.Delay(waitTime, cancellationToken);
                }
            }
            //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
        }
    }
}