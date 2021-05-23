using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.Infrastructure;
using IFramework.KafkaTools.Models;
using IFramework.MessageQueue;
using Microsoft.AspNetCore.Mvc;
using ConsumerConfig = Confluent.Kafka.ConsumerConfig;
using ProducerConfig = Confluent.Kafka.ProducerConfig;
using TopicPartitionOffset = Confluent.Kafka.TopicPartitionOffset;

namespace IFramework.KafkaTools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaController : ControllerBase
    {
        private const string MaintainerGroup = "MaintainerGroup";
        private static readonly ConcurrentDictionary<string, IProducer<string, string>> Producers = new ConcurrentDictionary<string, IProducer<string, string>>();

        [HttpPost("offset")]
        public void CommitOffset([FromBody] CommitOffsetRequest request)
        {
            using (var consumer = GetConsumer(request.Broker, request.Group))
            {
                var offsets = request.Offsets
                                     .Select(offset => new TopicPartitionOffset(new TopicPartition(offset.Topic,
                                                                                                   offset.Partition),
                                                                                offset.Offset))
                                     .ToArray();
                consumer.Commit(offsets);
            }
        }

        private IConsumer<string, string> GetConsumer(string broker, string group)
        {
            var consumerConfiguration = new ConsumerConfig
            {
                GroupId = group,
                ClientId = $"KafkaTools.{group}",
                EnableAutoCommit = false,
                //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                SocketNagleDisable = true,
                //{"statistics.interval.ms", 60000},
                BootstrapServers = broker
            };
            return new ConsumerBuilder<string, string>(consumerConfiguration).Build();
        }


        private IProducer<string, string> GetProducer(string broker)
        {
            return Producers.GetOrAdd($"{broker}", key =>
            {
                var producerConfiguration = new ProducerConfig
                {
                    BootstrapServers = broker,
                    Acks = Acks.Leader,
                    SocketNagleDisable = true
                };
                return new ProducerBuilder<string, string>(producerConfiguration).Build();
            });
        }

        [HttpPost("produce")]
        public async Task<object> ProduceMessage([FromBody] ProduceMessage produceMessage)
        {
            var producer = GetProducer(produceMessage.Broker);
            var result = await producer.ProduceAsync(produceMessage.Topic, new Message<string, string>
            {
                Key = produceMessage.Key,
                Value = new
                {
                    Headers = produceMessage.MessageHeaders,
                    Payload = produceMessage.MessagePayload.ToJson()
                }.ToJson()
            });
            return result;
        }

        [HttpGet("messageByOffset")]
        public dynamic GetMessageByOffset([FromQuery]MessageQueryRequest request)
        {
            using (var consumer = GetConsumer(request.Broker, MaintainerGroup))
            {
                var topicPartitionOffset = new TopicPartitionOffset(new TopicPartition(request.Topic,
                                                                                       request.Partition),
                                                                    request.Offset);
                consumer.Assign(topicPartitionOffset);
                consumer.Seek(topicPartitionOffset);
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                return new
                {
                    result?.Partition,
                    result?.Topic, 
                    result?.Offset,
                    result?.Message
                };
            }
        }

        [HttpPost("produceByOffset")]
        public async Task<object> ProduceByOffset([FromBody] MessagePostRequest request)
        {
            ConsumeResult<string, string> consumeResult = null;
            using (var consumer = GetConsumer(request.Broker, MaintainerGroup))
            {
                var topicPartitionOffset = new TopicPartitionOffset(new TopicPartition(request.Topic,
                                                                                       request.Partition),
                                                                    request.Offset);
                consumer.Assign(topicPartitionOffset);
                consumer.Seek(topicPartitionOffset);
                consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            }

            if (consumeResult == null)
            {
                throw new Exception($"message doesn't exist");
            }

            var producer = GetProducer(request.Broker);
            var produceResult = await producer.ProduceAsync(request.Topic, new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = consumeResult.Message.Value,
                Headers = consumeResult.Message.Headers
            });
            return produceResult;
        }
    }
}