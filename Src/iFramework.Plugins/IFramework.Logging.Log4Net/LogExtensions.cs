using System;
using log4net;
using log4net.Core;

namespace IFramework.Logging.Log4Net
{
    public static class LogExtensions
    {
        public static void Critical(this ILog log, object message, Exception exception)
        {
            log.Logger.Log((Type) null, Level.Critical, message, exception);
        }

        public static void Trace(this ILog log, object message, Exception exception)
        {
            log.Logger.Log((Type) null, Level.Trace, message, exception);
        }
    }
}
