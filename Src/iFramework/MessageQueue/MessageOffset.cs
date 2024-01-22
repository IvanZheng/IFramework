using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;

namespace IFramework.MessageQueue
{
    public class MessageOffset: ValueObject<MessageOffset>
    {
        public MessageOffset()
        {
            
        }
        public MessageOffset(string broker, string topic, int partition, long offset, object queueMessage = null)
        {
            Broker = broker;
            Topic = topic;
            Partition = partition;
            Offset = offset;
            _message = queueMessage;
        }

        public string Broker { get; protected set; }
        public string Topic { get; protected set; }
        public int Partition { get; protected set; }
        public long Offset { get; protected set; }
        private readonly object _message;

        public object GetMessage()
        {
            return _message;
        }

        public string SlidingDoorKey => SlidingDoor.GetSlidingDoorKey(Topic, Partition);
    }
}
