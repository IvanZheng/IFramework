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
        private static readonly ConcurrentDictionary<string, Consumer> Consumers = new ConcurrentDictionary<string, Consumer>();

        [HttpPost]
        public async Task<object> CommitOffset(CommitOffsetRequest request)
        {
            Consumer consumer = GetConsumer(request.Broker, request.Group);
            var offsets = request.Offsets
                                 .Select(offset => new TopicPartitionOffset(new TopicPartition(offset.Topic,
                                                                                               offset.Partition),
                                                                            offset.Offset))
                                 .ToArray();
            return await consumer.CommitAsync(offsets).ConfigureAwait(false);
        }

        private Consumer GetConsumer(string broker, string group)
        {
            return Consumers.GetOrAdd($"{broker}.{group}", key =>
            {
                var consumerConfiguration = new Dictionary<string, object>
                {
                    {"group.id", group},
                    {"client.id", $"KafkaTools.{group}"},
                    {"enable.auto.commit", false},
                    //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                    //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                    {"socket.nagle.disable", true},
                    //{"statistics.interval.ms", 60000},
                    {"bootstrap.servers", broker}
                };
                return new Consumer(consumerConfiguration);
            });
        }
    }
}