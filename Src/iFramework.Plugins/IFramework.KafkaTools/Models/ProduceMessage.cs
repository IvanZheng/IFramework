using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFramework.KafkaTools.Models
{
    public class ProduceMessage
    {
        public string Broker { get;set; }
        public string Topic { get; set; }
        public string Key { get; set; }
        public string Message { get;set; }
    }
}
