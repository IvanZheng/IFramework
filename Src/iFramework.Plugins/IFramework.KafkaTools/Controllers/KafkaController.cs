using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using IFramework.KafkaTools.Models;
using Microsoft.AspNetCore.Mvc;
using TopicPartitionOffset = Confluent.Kafka.TopicPartitionOffset;

namespace IFramework.KafkaTools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, Consumer<string, string>> Consumers = new ConcurrentDictionary<string, Consumer<string, string>>();

        [HttpPost]
        public void CommitOffset(CommitOffsetRequest request)
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
    }
}