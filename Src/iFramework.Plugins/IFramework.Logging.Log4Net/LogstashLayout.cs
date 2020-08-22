using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IFramework.Infrastructure;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace IFramework.Logging.Log4Net
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

        protected virtual object FormatMessageObject(object messageObject)
        {
            return messageObject is string ? messageObject : messageObject.ToJson();
        }

        private object GetJsonObject(LoggingEvent loggingEvent)
        {
            var additionalProperties = log4net.LogicalThreadContext
                                              .Properties[AdditionalPropertiesKey]?
                                              .ToJson()
                                              .ToJsonObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();

            var log = new JsonLog
            {
                Data = FormatMessageObject(loggingEvent.MessageObject),
                Thread = loggingEvent.ThreadName,
                Time = loggingEvent.TimeStamp
            };
            //log.LocationInfo = loggingEvent.LocationInformation;
            log.App = additionalProperties.TryGetValue(nameof(log.App), App)?.ToString();
            log.Module = additionalProperties.TryGetValue(nameof(log.Module), Module)?.ToString();
            log.Logger = additionalProperties.TryGetValue(nameof(log.Logger), loggingEvent.LoggerName).ToString();
            log.Host = Environment.MachineName;
            log.HostIp = Utility.GetLocalIpv4().ToString();
            log.LogLevel = loggingEvent.Level.ToString();


            foreach (DictionaryEntry loggingEventProperty in loggingEvent.Properties)
            {
                if (!(loggingEventProperty.Key?.ToString().StartsWith("log4net:") ?? true))
                {
                    log.Scope = log.Scope ?? new Dictionary<string, object>();
                    log.Scope.Add(loggingEventProperty.Key.ToString(), loggingEventProperty.Value);
                }
            }

            if (loggingEvent.ExceptionObject != null)
            {
                log.Exception = new LogException
                {
                    Class = loggingEvent.ExceptionObject.GetType().ToString(),
                    Message = loggingEvent.ExceptionObject.GetBaseException().Message,
                    StackTrace = loggingEvent.ExceptionObject.StackTrace
                };
            }
            //var logDict = log.ToJson().ToJsonObject<Dictionary<string, object>>();
            //additionalProperties.ForEach(p =>
            //{
            //    if (p.Key != nameof(App) && p.Key != nameof(Module))
            //    {
            //        logDict[p.Key] = p.Value;
            //    }
            //});
            return log;
        }
    }
}
