using System;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;
        private Func<object, Exception, string> _exceptionDetailsFormatter;


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
            string str = null;
            if (formatter != null)
            {
                str = formatter(state, exception);
            }
            if (exception != null && _exceptionDetailsFormatter != null)
            {
                str = _exceptionDetailsFormatter(str, exception);
            }
            if (string.IsNullOrEmpty(str) && exception == null)
            {
                return;
            }
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _log.Debug(str);
                    break;
                case LogLevel.Information:
                    _log.Info(str);
                    break;
                case LogLevel.Warning:
                    _log.Warn(str);
                    break;
                case LogLevel.Error:
                    _log.Error(str);
                    break;
                case LogLevel.Critical:
                    _log.Fatal(str);
                    break;
                default:
                    _log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                    _log.Info(str, exception);
                    break;
            }
        }

        public Log4NetLogger UsingCustomExceptionFormatter(Func<object, Exception, string> formatter)
        {
            var func = formatter;
            _exceptionDetailsFormatter = func ?? throw new ArgumentNullException(nameof(formatter));
            return this;
        }
    }
}