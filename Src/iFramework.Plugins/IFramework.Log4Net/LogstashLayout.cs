using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IFramework.Infrastructure;
using log4net.Core;
using log4net.Layout;

namespace IFramework.Log4Net
{
    public class LogstashLayout : LayoutSkeleton
    {
        private const string AdditionalPropertiesKey = "AdditionalProperties";
        public string App { get; set; }
        public string Module { get; set; }
        public LogstashLayout()
        {
            IgnoresException = false;
        }
        public override void ActivateOptions()
        {

        }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            var evt = GetJsonObject(loggingEvent);

            var message = evt.ToJson(useCamelCase: true, ignoreNullValue: true);

            writer.Write(message + Environment.NewLine + Environment.NewLine);
        }
        private object GetJsonObject(LoggingEvent loggingEvent)
        {
            var additionalProperties = log4net.LogicalThreadContext
                                              .Properties[AdditionalPropertiesKey]?
                                              .ToJson()
                                              .ToJsonObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();

            var log = new JsonLog
            {
                Message = loggingEvent.MessageObject
            };
            //log.LocationInfo = loggingEvent.LocationInformation;
            log.Thread = loggingEvent.ThreadName;
            log.Time = DateTime.Now;
            log.App = additionalProperties.TryGetValue(nameof(log.App), App)?.ToString();
            log.Module = additionalProperties.TryGetValue(nameof(log.Module), Module)?.ToString();
            log.Logger = additionalProperties.TryGetValue(nameof(log.Logger), loggingEvent.LoggerName).ToString();
            log.Host = Environment.MachineName;
            log.HostIp = Utility.GetLocalIpv4().ToString();
            log.LogLevel = loggingEvent.Level.ToString();

            if (loggingEvent.ExceptionObject != null)
            {
                log.Exception = new LogException
                {
                    Class = loggingEvent.ExceptionObject.GetType().ToString(),
                    Message = loggingEvent.ExceptionObject.Message,
                    StackTrace = loggingEvent.ExceptionObject.StackTrace
                };
            }
            var logDict = log.ToJson().ToJsonObject<Dictionary<string, object>>();
            additionalProperties.ForEach(p =>
            {
                if (p.Key != nameof(App) && p.Key != nameof(Module))
                {
                    logDict[p.Key] = p.Value;
                }
            });
            return logDict;
        }
    }
}
