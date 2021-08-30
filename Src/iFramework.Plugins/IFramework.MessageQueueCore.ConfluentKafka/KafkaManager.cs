using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ConfluentKafka
{
    public class KafkaManager:IDisposable
    {
        public Lazy<IAdminClient> AdminClient => new Lazy<IAdminClient>(() => new AdminClientBuilder(new Confluent.Kafka.ConsumerConfig(ToStringExtensions(ClientOptions.Extensions))
                                {
                                    SocketNagleDisable = true,
                                    BootstrapServers = ClientOptions.BrokerList
                                }).Build());

        public ConcurrentDictionary<string, IConsumer<string, string>> Consumers = new ConcurrentDictionary<string, IConsumer<string, string>>();
        public KafkaClientOptions ClientOptions { get; }
        public KafkaManager(IOptions<KafkaClientOptions> options)
        {
            ClientOptions = options.Value;
        }

        private Dictionary<string, string> ToStringExtensions(Dictionary<string, object> objectExtensions)
        {
            var extensions = new Dictionary<string, string>();
            foreach (var keyValuePair in objectExtensions)
            {
                extensions[keyValuePair.Key] = keyValuePair.Value?.ToString();
            }

            return extensions;
        }

        private IConsumer<string, string> GetConsumer(string broker, string group)
        {
            return Consumers.GetOrAdd(group, key =>
            {
                var consumerConfiguration = new Confluent.Kafka.ConsumerConfig(ToStringExtensions(ClientOptions.Extensions))
                {
                    GroupId = group,
                    ClientId = $"KafkaTools.{group}",
                    //{"socket.blocking.max.ms", ConsumerConfig["socket.blocking.max.ms"] ?? 50},
                    //{"fetch.error.backoff.ms", ConsumerConfig["fetch.error.backoff.ms"] ?? 50},
                    SocketNagleDisable = true,
                    //{"statistics.interval.ms", 60000},
                    BootstrapServers = broker
                };
            
                return new ConsumerBuilder<string, string>(consumerConfiguration).Build();
            });
        }

        public ConsumerOffset[] GetTopicInfo(string topic, string group)
        {
            var timeout = TimeSpan.FromSeconds(15);
            var topicMetadata = AdminClient.Value.GetMetadata(topic, timeout);
            var consumerGroup = GetConsumer(ClientOptions.BrokerList, group);
            var committedOffsets = consumerGroup.Committed(topicMetadata.Topics.SelectMany(t => t.Partitions.Select(p => new TopicPartition(t.Topic, p.PartitionId))), timeout);
            var positions = committedOffsets.Select(o => new ConsumerOffset(o.TopicPartition.Topic,
                                                                            o.TopicPartition.Partition.Value,
                                                                            o.Offset,
                                                                            consumerGroup.QueryWatermarkOffsets(o.TopicPartition, timeout)))
                                            .ToArray();
            
            return positions;
        }

        public void Dispose()
        {
            if (AdminClient.IsValueCreated)
            {
                AdminClient.Value.Dispose();
            } 
            Consumers.Values.ForEach(c => c.Dispose());
        }
    }
}
