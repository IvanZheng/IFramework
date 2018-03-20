using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue
{
    public class MessageOffset
    {
        public MessageOffset()
        {
            
        }
        public MessageOffset(string broker, int partition, long offset)
        {
            Broker = broker;
            Partition = partition;
            Offset = offset;
        }

        public string Broker { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
    }
}
