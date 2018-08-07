using System;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure
{
    public static class LoggerExtensions
    {
        public static Func<object, Exception, string> MessageFormatter = (o, e) => o.ToJson();
        public static void LogDebug(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Debug, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogDebug(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Debug, new EventId(), state, null, formatter ?? MessageFormatter);

        }
        public static void LogTrace(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Trace, new EventId(), state, exception, formatter ?? MessageFormatter);
        }
        public static void LogTrace(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Trace, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogInformation(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Information, new EventId(), state, exception, formatter ?? MessageFormatter);
        }
        public static void LogInformation(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Information, new EventId(), state, null, formatter ?? MessageFormatter);
        }
        public static void LogWarning(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Warning, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogWarning(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Warning, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogError(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Error, new EventId(), state, exception, formatter ?? MessageFormatter);
        }


        public static void LogCritical(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Critical, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogCritical(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Critical, new EventId(), state, null, formatter ?? MessageFormatter);
        }
    }
}
