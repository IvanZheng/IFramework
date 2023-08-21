using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue.RocketMQ
{
    public class RocketMQClientOptions
    {
        public string Endpoints { get; set; }
        public Dictionary<string, object> Extensions { get; set; }
    }
}
