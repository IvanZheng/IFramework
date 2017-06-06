using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaProducer
    {
        private readonly ILogger _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaProducer).Name);
        private readonly Producer<string, KafkaMessage> _producer;
        private readonly string _topic;
        private readonly string _brokerList;

        public KafkaProducer(string topic, string brokerList)
        {
            _brokerList = brokerList;
            _topic = topic;
            var producerConfiguration = new Dictionary<string, object>
            {
                { "bootstrap.servers", _brokerList },
             //   {"delivery.report.only.error", true},
                {"request.required.acks", 1},
               // {"queue.buffering.max.ms", 1}
            };

            _producer = new Producer<string, KafkaMessage>(producerConfiguration, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());
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

        public Message<string, KafkaMessage> Send(string key, KafkaMessage message)
        {
            var result = _producer.ProduceAsync(_topic, key, message).Result;
            //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
            return result;
        }

        public async Task<Message<string, KafkaMessage>> SendAsync(string key, KafkaMessage message)
        {
            var result = await _producer.ProduceAsync(_topic, key, message);
            //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
            return result;
        }
    }
}