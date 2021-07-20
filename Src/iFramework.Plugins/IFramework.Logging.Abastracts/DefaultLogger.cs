using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Abstracts
{
    public class DefaultLogger : ILogger
    {
        private readonly LoggerProvider _provider;

        public DefaultLogger(LoggerProvider provider,
                             string name = null,
                             LogLevel minLevel = LogLevel.Information)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Name = name;
            MinLevel = minLevel;
        }

        public string Name { get; }
        protected LogLevel MinLevel { get; set; }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel < MinLevel)
            {
                return false;
            }

            return logLevel >= MinLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _provider.BeginScope(state);
        }

        public virtual void Log<TState>(LogLevel logLevel,
                                        EventId eventId,
                                        TState state,
                                        Exception exception,
                                        Func<TState, Exception, string> formatter)
        {
            try
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var logEvent = GetLogEvent(logLevel,
                                           eventId,
                                           state,
                                           exception,
                                           formatter);

                _provider.ProcessLog(logEvent);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        protected virtual LogEvent GetLogEvent<TState>(LogLevel logLevel,
                                                       EventId eventId,
                                                       TState state,
                                                       Exception exception,
                                                       Func<TState, Exception, string> formatter)
        {
            var scopeData = _provider.CurrentScope?.GetLogProperties();

            var log = new LogEvent
            {
                Level = logLevel,
                Logger = Name,
                Timestamp = DateTime.Now,
                State = AsLoggableValue(state, formatter),
                Exception = exception,
                Scope = scopeData
            };
            return log;
        }

        private static object AsLoggableValue<TState>(
            TState state,
            Func<TState, Exception, string> formatter)
        {
            object obj = state;
            if (formatter != null)
            {
                obj = formatter(state, null);
            }

            return obj;
        }
    }
}