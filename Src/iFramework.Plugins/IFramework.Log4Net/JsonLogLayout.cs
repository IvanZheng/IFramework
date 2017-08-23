using System;
using System.Globalization;
using System.IO;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using log4net.Core;
using log4net.Layout;

namespace IFramework.Log4Net
{
    public class JsonLogLayout : LayoutSkeleton
    {
        public string App { get; set; }
        public string Module { get; set; }
        public JsonLogLayout()
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

            writer.Write(message + "\r\n");
        }
        private object GetJsonObject(LoggingEvent loggingEvent)
        {
            var log = loggingEvent.MessageObject as JsonLogBase ?? new JsonLogBase
            {
                Message = loggingEvent.MessageObject
            };
            var stackFrame = loggingEvent.LocationInformation.StackFrames[1];
            log.Method = log.Method ?? stackFrame.Method.Name;
            log.Thread = log.Thread ?? loggingEvent.ThreadName;
            log.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            log.App = log.App ?? App;
            log.Module = log.Module ?? log4net.LogicalThreadContext.Properties[nameof(log.Module)]?.ToString() ?? Module;
            log.Host = log.Host ?? Environment.MachineName;
            log.Ip = Utility.GetLocalIPV4().ToString();
            log.LogLevel = loggingEvent.Level.ToString();
            log.Logger = loggingEvent.LoggerName;

            if (loggingEvent.ExceptionObject != null)
            {
                log.Exception = new Infrastructure.Logging.LogException
                {
                    Class = loggingEvent.ExceptionObject.GetType().ToString(),
                    Message = loggingEvent.ExceptionObject.Message,
                    StackTrace = loggingEvent.ExceptionObject.StackTrace
                };
            }
            return log;
        }
    }
}
