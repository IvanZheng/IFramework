using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaProducer : BaseKafkaProducer, IMessageProducer
    {
        public KafkaProducer(string topic,
                             string brokerList,
                             ProducerConfig config = null)
            : base(topic, brokerList, config) { }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var message = ((MessageContext)messageContext).PayloadMessage.ToJson(processDictionaryKeys: false);
            var topic = Configuration.Instance.FormatMessageQueueName(messageContext.Topic);
            return SendAsync(topic, messageContext.Key ?? messageContext.MessageId, message, cancellationToken);
        }

        protected override Message<string, string> BuildMessage(string topic, string key, string value)
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = value
            };
            return kafkaMessage;
        }
    }

    public abstract class BaseKafkaProducer
    {
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(typeof(KafkaProducer));
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;

        protected BaseKafkaProducer(string topic,
                                    string brokerList,
                                    ProducerConfig config = null)
        {
            Config = config ?? new ProducerConfig();

            _topic = topic;
            var producerConfiguration = new Confluent.Kafka.ProducerConfig(Config.ToStringExtensions())
            {
                BootstrapServers = brokerList,
                SocketNagleDisable = true,
                Acks = (Acks)(Config["request.required.acks"] ?? 1),
                //SecurityProtocol = Config["security.protocol"]?.ToString().ToEnum<SecurityProtocol>(),
                //SaslMechanism = Config["sasl.mechanism"]?.ToString().ToEnum<SaslMechanism>(),
                //SaslUsername = Config["sasl.username"]?.ToString(),
                //SaslPassword = Config["sasl.password"]?.ToString(),
                //SslCaLocation = Config["ssl.ca.location"]?.ToString()
                //{"socket.blocking.max.ms", Config["socket.blocking.max.ms"] ?? 50},
                //{"queue.buffering.max.ms", Config["queue.buffering.max.ms"] ?? 50}
            };

            _producer = new ProducerBuilder<string, string>(producerConfiguration).Build();
            //_producer.OnError += _producer_OnError;
        }

        public ProducerConfig Config { get; }

        protected abstract Message<string, string> BuildMessage(string topic, string key, string value);

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

        public async Task<DeliveryResult<string, string>> SendAsync(string topic, string key, string message, CancellationToken cancellationToken)
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
                    var kafkaMessage = BuildMessage(topic, key, message);
                    var result = await _producer.ProduceAsync(topic,
                                                              kafkaMessage,
                                                              cancellationToken)
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