using Kafka.Client.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.MSKafka
{
    public class TopicClient
    {
        Producer _producer;
        public TopicClient(Producer producer)
        {
            _producer = producer;
        }
    }
}
