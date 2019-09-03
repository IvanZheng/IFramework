using System;
using log4net.Core;

namespace IFramework.Logging.Log4Net
{
    public class JsonLog
    {
        public string Host { get; set; }
        public string HostIp { get; set; }
        public string App { get; set; }
        public string Module { get; set; }
        public string UserName { get; set; }
        public string Logger { get; set; }
        public string LogLevel { get; set; }
        public DateTime Time { get; set; }
        public LocationInfo LocationInfo { get; set; }
        public string Thread { get; set; }
        public string Target { get; set; }
        public object Data { get; set; }
        public LogException Exception { get; set; }
    }

    public class LogException
    {
        public string Class { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
