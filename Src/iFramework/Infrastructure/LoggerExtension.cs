using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IFramework.Infrastructure;

namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        private static string MessageFormatter(object o, Exception e)
        {
            string message;
            if (o is Exception ex)
            {
                message = new {ex.Message, ex.StackTrace, Class = ex.GetType().Name}.ToJson();
            }
            else
            {
                message = o is string s ? s : o.ToJson();
            }

            return message;
        }

        public static void LogDebug(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Debug, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogDebug(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Debug, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogDebug(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Debug, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogTrace(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Trace, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogTrace(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Trace, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogTrace(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Trace, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogInformation(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Information, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogInformation(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Information, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogInformation(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Information, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogWarning(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Warning, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogWarning(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Warning, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogWarning(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Warning, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogError(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Error, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogError(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Error, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogError(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Error, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static void LogCritical(this ILogger logger, Exception exception, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Critical, new EventId(), null, exception, formatter ?? MessageFormatter);
        }

        public static void LogCritical(this ILogger logger, Exception exception, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Critical, new EventId(), state, exception, formatter ?? MessageFormatter);
        }

        public static void LogCritical(this ILogger logger, object state, Func<object, Exception, string> formatter = null)
        {
            logger.Log(LogLevel.Critical, new EventId(), state, null, formatter ?? MessageFormatter);
        }

        public static ILogger[] GetLoggers(this ILoggerFactory loggerFactory)
        {
            dynamic loggerSet = loggerFactory.GetType()
                                           .GetField("_loggers", BindingFlags.Instance | BindingFlags.NonPublic)
                                           ?.GetValue(loggerFactory);
          
            dynamic loggersArray =  loggerSet?.GetType().GetProperty("Values")?.GetValue(loggerSet);
            object[] loggers = loggersArray != null ? Enumerable.ToArray(loggersArray) : null;
            return loggers?.Cast<ILogger>().ToArray();
        }

        public static ILogger GetInternalLogger(this ILogger logger)
        {
            if (logger.GetType().IsGenericType)
            {
                logger = logger.GetType()
                               .GetField("_logger", 
                                         BindingFlags.Instance | BindingFlags.NonPublic)
                               ?.GetValue(logger) as ILogger;
            }
           
            if (logger == null)
            {
                throw new Exception("Can't get internal logger!");
            }

            return logger;
        }

        public static LoggerInfo GetInfo(this ILogger logger)
        {
            logger = logger.GetInternalLogger();
            var loggers = logger.GetType()
                                .GetProperty("MessageLoggers")
                                ?.GetValue(logger) as Array;
            if (loggers == null)
            {
                throw new Exception("Can't get loggerInformation");
            }

            LoggerInfo loggerInfo = new LoggerInfo();
            for (int i = 0;  i < loggers.Length; i ++)
            {
                var messageLogger = loggers.GetValue(i);
                if (messageLogger != null)
                {
                    var value = messageLogger.GetType()
                                     .GetProperty("MinLevel")
                                     ?.GetValue(messageLogger) ?? LogLevel.None;
                    var currentLevel = (LogLevel)value;
                    if (currentLevel < loggerInfo.MinLevel)
                    {
                        loggerInfo = new LoggerInfo(messageLogger.GetType()
                                                                     .GetProperty("Category")
                                                                     ?.GetValue(messageLogger)
                                                                     ?.ToString(),
                                                    currentLevel);
                    }
                }
            }

            return loggerInfo;
        }

        public static void SetMinLevel(this ILogger logger, LogLevel minLevel)
        {
            logger = logger.GetInternalLogger();
            var loggers = logger.GetType()
                                .GetProperty("MessageLoggers")
                                ?.GetValue(logger) as Array;
            if (loggers == null)
            {
                throw new Exception("Can't get loggerInformation");
            }
            for (int i = 0;  i < loggers.Length; i ++)
            {
                var messageLogger = loggers.GetValue(i);
                if (messageLogger != null)
                {
                    var minLevelField = messageLogger.GetType().GetRuntimeFields().FirstOrDefault(f => f.Name.StartsWith("<MinLevel>"));
                    if (minLevelField != null)
                    {
                        minLevelField.SetValue(messageLogger, minLevel);
                        loggers.SetValue(messageLogger, i);
                    }
                }
            }
        }
    }

    public class LoggerInfo
    {
        public string Name { get; set; }
        public LogLevel MinLevel { get; set; }

        public LoggerInfo()
        {
            MinLevel = LogLevel.None;
        }
        public LoggerInfo(string name, LogLevel minLevel)
        {
            Name = name;
            MinLevel = minLevel;
        }
    }
}