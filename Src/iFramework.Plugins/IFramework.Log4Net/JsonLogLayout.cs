using System;
using System.Globalization;
using System.IO;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using log4net.Core;
using log4net.Layout;

namespace IFramework.Log4Net
{
    public class JsonLogLayout: LayoutSkeleton
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

            var message = evt.ToJson(useCamelCase:true, ignoreNullValue: true);

            writer.Write(message + "\r\n");
        }
        private JsonLogBase GetJsonObject(LoggingEvent loggingEvent)
        {
            var log = loggingEvent.MessageObject as JsonLogBase ?? new JsonLogBase
            {
                Message = loggingEvent.RenderedMessage
            };
            var stackFrame = loggingEvent.LocationInformation.StackFrames[1];
            log.Method = log.Method ?? stackFrame.Method.Name;
            log.Thread = log.Thread ?? loggingEvent.ThreadName;
            log.Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            log.App = log.App ?? App;
            log.Module = log.Module ?? Module;
            log.Host = log.Host ?? Environment.MachineName;
            log.LogLevel = loggingEvent.Level.ToString();
            log.Logger = loggingEvent.LoggerName;
            

            //var obj = new LogstashEvent
            //{
            //    version = 1,
            //    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            //    app = App,
            //    source_host = Environment.MachineName,
            //    thread_name = loggingEvent.ThreadName,
            //    @class = loggingEvent.LocationInformation.ClassName,
            //    method = loggingEvent.LocationInformation.MethodName,
            //    line_number = loggingEvent.LocationInformation.LineNumber,
            //    level = loggingEvent.Level.ToString(),
            //    logger_name = loggingEvent.LoggerName,
            //    message = loggingEvent.RenderedMessage
            //};

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
