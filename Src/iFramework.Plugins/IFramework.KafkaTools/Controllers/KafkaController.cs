using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using IFramework.KafkaTools.Models;
using IFramework.MessageQueue;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IFramework.KafkaTools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaController : ControllerBase
    {
        static ConcurrentDictionary<string, Consumer> Consumers = new ConcurrentDictionary<string, Consumer>();
        [HttpPost]
        public async Task<object> CommitOffset(CommitOffsetRequest request)
        {
            Consumer consumer = GetConsumer(request.Broker, request.Group);
            return await consumer.CommitAsync(new []
            {
                new TopicPartitionOffset(new TopicPartition(request.Topic, request.Partition), request.Offset)
            }).ConfigureAwait(false);
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
                    {"bootstrap.servers", broker},
              
                };
                return new Consumer(consumerConfiguration);
            });
        }
    }
}