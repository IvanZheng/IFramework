using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
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
        private static readonly ConcurrentDictionary<string, Consumer<string, string>> Consumers = new ConcurrentDictionary<string, Consumer<string, string>>();
        private static readonly ConcurrentDictionary<string, Producer<string, string>> Producers = new ConcurrentDictionary<string, Producer<string, string>>();

        [HttpPost("offset")]
        public void CommitOffset([FromBody]CommitOffsetRequest request)
        {
            var consumer = GetConsumer(request.Broker, request.Group);
            var offsets = request.Offsets
                                 .Select(offset => new TopicPartitionOffset(new TopicPartition(offset.Topic,
                                                                                               offset.Partition),
                                                                            offset.Offset))
                                 .ToArray();
            consumer.Commit(offsets);
        }

        private Consumer<string, string> GetConsumer(string broker, string group)
        {
            return Consumers.GetOrAdd($"{broker}.{group}", key =>
            {
                var consumerConfiguration = new Dictionary<string, string>
                {
                    {"group.id", group},
                    {"client.id", $"KafkaTools.{group}"},
                    {"enable.auto.commit", false.ToString().ToLower()},
                    //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                    //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                    {"socket.nagle.disable", true.ToString().ToLower()},
                    //{"statistics.interval.ms", 60000},
                    {"bootstrap.servers", broker}
                };
                return new Consumer<string, string>(consumerConfiguration);
            });
        }


        private Producer<string, string> GetProducer(string broker)
        {
            return Producers.GetOrAdd($"{broker}", key =>
            {
                var producerConfiguration = new Dictionary<string, string>
                {
                    {"bootstrap.servers", broker},
                    {"request.required.acks", "1"},
                    {"socket.nagle.disable", true.ToString().ToLower()}
                };
                return new Producer<string, string>(producerConfiguration);
            });
        }

        [HttpPost("produce")]
        public async Task<object> ProduceMessage([FromBody]ProduceMessage produceMessage)
        {
            var producer = GetProducer(produceMessage.Broker);
            var result = await producer.ProduceAsync(produceMessage.Topic, new Message<string, string>
            {
                Key = produceMessage.Key,
                Value = produceMessage.Message
            });
            return result;
        }


    }
}