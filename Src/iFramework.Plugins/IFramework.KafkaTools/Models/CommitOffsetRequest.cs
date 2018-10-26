using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFramework.KafkaTools.Models
{
    public class CommitOffsetRequest
    {
        public string Broker { get; set; }
        public string Group { get; set; }

        public TopicPartitionOffset[] Offsets { get; set; }
    }


    public class TopicPartitionOffset
    {
        public string Topic { get;set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
    }
}
