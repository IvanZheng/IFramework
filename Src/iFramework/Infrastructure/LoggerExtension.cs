using System;
using IFramework.Infrastructure;

namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static Func<object, Exception, string> MessageFormatter = (o, e) =>
        {
            string message;
            if (o is Exception ex)
            {
                message = new {ex.Message, ex.StackTrace, Class = ex.GetType().Name}.ToJson();
            }
            else
            {
                
                message =  o is string ? o.ToString() : o.ToJson();
            }
            return message;
        };
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

        public static void LogError(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Error, new EventId(), state, null, formatter ?? MessageFormatter);
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
