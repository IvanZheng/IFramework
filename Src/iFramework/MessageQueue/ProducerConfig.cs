using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue
{
    public class ProducerConfig
    {
        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();

        public object this[string key]
        {
            get => Extensions.TryGetValue(key, out var value) ? value : null;
            set => Extensions[key] = value;
        }
    }
}
