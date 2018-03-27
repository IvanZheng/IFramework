using System;
using log4net;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;

        public Log4NetLogger(string repositoryName, string name)
        {
            _log = LogManager.GetLogger(repositoryName, name);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return _log.IsDebugEnabled;
                case LogLevel.Information:
                    return _log.IsInfoEnabled;
                case LogLevel.Warning:
                    return _log.IsWarnEnabled;
                case LogLevel.Error:
                    return _log.IsErrorEnabled;
                case LogLevel.Critical:
                    return _log.IsFatalEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string str = formatter(state, exception);
            if (string.IsNullOrEmpty(str) && exception == null)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _log.Debug(str, exception);
                    break;
                case LogLevel.Information:
                    _log.Info(str, exception);
                    break;
                case LogLevel.Warning:
                    _log.Warn(str, exception);
                    break;
                case LogLevel.Error:
                    _log.Error(str, exception);
                    break;
                case LogLevel.Critical:
                    _log.Fatal(str, exception);
                    break;
                default:
                    _log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.", exception);
                    _log.Info(str, exception);
                    break;
            }
        }
    }
}