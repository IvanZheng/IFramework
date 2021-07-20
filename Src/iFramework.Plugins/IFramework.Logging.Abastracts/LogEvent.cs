using System;
using System.Collections.Generic;
using IFramework.Infrastructure;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Abstracts
{
    public class LogEvent
    {
        public string Logger { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public object State { get; set; }
        public Exception Exception { get; set; }
        public Dictionary<string, object> Scope { get; set; }

        public override string ToString()
        {
            return this.ToJson();
        }

        public IDictionary<string, string> GetContents()
        {
            var contents = new Dictionary<string, string>
            {
                ["Message"] = FormatState(State),
                ["Level"] = Level.ToString()
            };
            if (Exception != null)
            {
                contents["Exception"] = Exception.Message;
                contents["StackTrace"] = Exception.StackTrace;
            }

            if (Scope != null)
            {
                foreach (var scope in Scope)
                {
                    contents[scope.Key] = scope.Value?.ToString() ?? string.Empty;
                }
            }
            return contents;
        }

        private string FormatState(object state)
        {
            if (state is string || state.GetType().IsPrimitive || state is Guid)
            {
                return state.ToString();
            }

            return state.ToJson();
        }
    }
}