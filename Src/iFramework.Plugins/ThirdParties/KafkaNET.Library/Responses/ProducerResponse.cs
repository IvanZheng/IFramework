using System.Collections.Generic;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Responses
{
    public class ProducerResponseStatus
    {
        public ErrorMapping Error { get; set; }
        public long Offset { get; set; }

        public override string ToString()
        {
            return string.Format("Error:{0} Offset:{1}", Error, Offset);
        }
    }

    public class ProducerResponse
    {
        public ProducerResponse(int correlationId, Dictionary<TopicAndPartition, ProducerResponseStatus> statuses)
        {
            CorrelationId = correlationId;
            Statuses = statuses;
        }

        public int CorrelationId { get; set; }
        public Dictionary<TopicAndPartition, ProducerResponseStatus> Statuses { get; set; }

        public class Parser : IResponseParser<ProducerResponse>
        {
            public ProducerResponse ParseFrom(KafkaBinaryReader reader)
            {
                var size = reader.ReadInt32();
                var correlationId = reader.ReadInt32();
                var topicCount = reader.ReadInt32();

                var statuses = new Dictionary<TopicAndPartition, ProducerResponseStatus>();
                for (var i = 0; i < topicCount; ++i)
                {
                    var topic = reader.ReadShortString();
                    var partitionCount = reader.ReadInt32();
                    for (var p = 0; p < partitionCount; ++p)
                    {
                        var partitionId = reader.ReadInt32();
                        var error = reader.ReadInt16();
                        var offset = reader.ReadInt64();
                        var topicAndPartition = new TopicAndPartition(topic, partitionId);

                        statuses.Add(topicAndPartition, new ProducerResponseStatus
                        {
                            Error = ErrorMapper.ToError(error),
                            Offset = offset
                        });
                    }
                }

                return new ProducerResponse(correlationId, statuses);
            }
        }
    }
}