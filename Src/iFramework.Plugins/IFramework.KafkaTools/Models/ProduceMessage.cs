using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IFramework.KafkaTools.Models
{
    public class ProduceMessage
    {
        public string Broker { get;set; }
        public string Topic { get; set; }
        public string Key { get; set; }
        public JObject MessageHeaders { get;set; }
        public JObject MessagePayload { get;set; }
    }
}
