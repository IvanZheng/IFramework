using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.Infrastructure;
using IFramework.KafkaTools.Models;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.AspNetCore.Mvc;
using TopicPartitionOffset = Confluent.Kafka.TopicPartitionOffset;

namespace IFramework.KafkaTools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, Producer<string, string>> Producers = new ConcurrentDictionary<string, Producer<string, string>>();

        [HttpPost("offset")]
        public void CommitOffset([FromBody]CommitOffsetRequest request)
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

        private Consumer<string, string> GetConsumer(string broker, string group)
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


        private Producer<string, string> GetProducer(string broker)
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
        public async Task<object> ProduceMessage([FromBody]ProduceMessage produceMessage)
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


    }
}