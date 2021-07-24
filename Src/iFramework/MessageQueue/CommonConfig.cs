using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue
{
    public class CommonConfig
    {
        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();

        public object this[string key]
        {
            get => Extensions.TryGetValue(key, out var value) ? value : null;
            set => Extensions[key] = value;
        }

        public Dictionary<string, string> ToStringExtensions()
        {
            var extensions = new Dictionary<string, string>();
            foreach (var keyValuePair in Extensions)
            {
                extensions[keyValuePair.Key] = keyValuePair.Value?.ToString();
            }

            return extensions;
        }
    }
}
