﻿using System;
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

        public string Get(string key)
        {
            return this[key]?.ToString();
        }

        public T Get<T>(string key)
        {
            var value = this[key];
            if (value != null)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return default;
        }
    }
}
