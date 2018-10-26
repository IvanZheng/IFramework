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
        public MessageOffset(string broker, string topic, int partition, long offset)
        {
            Broker = broker;
            Topic = topic;
            Partition = partition;
            Offset = offset;
        }

        public string Broker { get; set; }
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }

        public string SlidingDoorKey => SlidingDoor.GetSlidingDoorKey(Topic, Partition);
    }
}
