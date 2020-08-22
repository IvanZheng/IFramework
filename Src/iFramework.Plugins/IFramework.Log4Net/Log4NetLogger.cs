using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;
        private readonly Log4NetProviderOptions _options;

        public Log4NetLogger(string repositoryName, string name, Log4NetProviderOptions options)
        {
            _options = options;
            _log = LogManager.GetLogger(repositoryName, name);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!_options.EnableScope)
            {
                return NullScope.Instance;
            }

            if (_options.CaptureMessageProperties && state is IEnumerable<KeyValuePair<string, object>> messageProperties)
            {
                return ScopeProperties.CreateFromState(messageProperties);
            }

            return NestedDiagnosticsLogicalContext.Push(state);
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

            //if (formatter == null)
            //{
            //    throw new ArgumentNullException(nameof(formatter));
            //}

            //string str = formatter(state, exception);

            if (state == null && exception == null)
            {
                return;
            }

            //UnderlyingSystemType	{Microsoft.Extensions.Logging.LoggerMessage+LogValues`3[System.Type,System.String,System.Exception]}	System.Type {System.RuntimeType}

            object log = null;
            if (state is Exception ex)
            {
                log = formatter(default, ex);//new {ex.GetBaseException().Message, ex.StackTrace, Class = ex.GetType().Name};
            }
            else //(state != null && (state is FormattedLogValues || state.GetType().Name.StartsWith("LogValues")))
            {
                log = formatter(state, exception);
                //if (logValues.Count == 1 && logValues[0].Value is string)
                //{
                //    log = logValues[0].Value;
                //}
            }

            if (_options.EnableScope)
            {
                var scopeMessages = NestedDiagnosticsLogicalContext.GetAllMessages()
                                                                   ?
                                                                   .ToList();
                if (scopeMessages?.Count > 0)
                {
                    log = new {scopeMessages, body = log};
                }
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                    _log.Trace(log, exception);
                    break;
                case LogLevel.Debug:
                    _log.Debug(log, exception);
                    break;
                case LogLevel.Information:
                    _log.Info(log, exception);
                    break;
                case LogLevel.Warning:
                    _log.Warn(log, exception);
                    break;
                case LogLevel.Error:
                    _log.Error(log, exception);
                    break;
                case LogLevel.Critical:
                    string criticalLevelWith = _options.OverrideCriticalLevelWith;
                    if (!string.IsNullOrEmpty(criticalLevelWith) && criticalLevelWith.Equals(LogLevel.Critical.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        _log.Critical(log, exception);
                        break;
                    }

                    _log.Fatal(log, exception);
                    break;
                default:
                    _log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                    _log.Info(log, exception);
                    break;
            }
        }

        private class ScopeProperties : IDisposable
        {
            private List<IDisposable> _properties;

            /// <summary>
            ///     Properties, never null and lazy init
            /// </summary>
            private List<IDisposable> Properties => _properties ?? (_properties = new List<IDisposable>());

            public void Dispose()
            {
                var properties = _properties;
                if (properties != null)
                {
                    _properties = null;
                    foreach (var property in properties)
                    {
                        try
                        {
                            property.Dispose();
                        }
                        catch (Exception)
                        {
                            //InternalLogger.Trace(ex, "Exception in Dispose property {0}", property);
                        }
                    }
                }
            }


            public static IDisposable CreateFromState(IEnumerable<KeyValuePair<string, object>> messageProperties)
            {
                var scope = new ScopeProperties();
                //var stateString = string.Empty;
                foreach (var property in messageProperties.ToArray())
                {
                    if (string.IsNullOrEmpty(property.Key))
                    {
                        continue;
                    }

                    //stateString += $"{property.Key}:{property.Value} ";
                    scope.AddProperty(property.Key, property.Value);
                }

                scope.AddDispose(NestedDiagnosticsLogicalContext.Push(messageProperties));
                return scope;
            }

            public void AddDispose(IDisposable disposable)
            {
                Properties.Add(disposable);
            }

            public void AddProperty(string key, object value)
            {
                AddDispose(new ScopeProperty(key, value));
            }

            private class ScopeProperty : IDisposable
            {
                private readonly string _key;

                public ScopeProperty(string key, object value)
                {
                    _key = key;
                    MappedDiagnosticsLogicalContext.Set(key, value);
                }

                public void Dispose()
                {
                    MappedDiagnosticsLogicalContext.Remove(_key);
                }
            }
        }
    }
}