using IFramework.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using IFramework.Logging.Abstracts;

namespace IFramework.Logging.Abstracts
{
    public class LoggerScope:IDisposable
    {
        private bool _disposed;
        private readonly LoggerProvider _provider;
        private readonly object _state;

        public LoggerScope(LoggerProvider provider, object state)
        {
            _provider = provider;
            _state = state;
            _state = state;
            Parent = _provider.CurrentScope;
            _provider.CurrentScope = this;
        }
        public LoggerScope Parent { get; }



        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            for (var loggerScope = _provider.CurrentScope; loggerScope != null; loggerScope = loggerScope.Parent)
            {
                if (loggerScope == this)
                {
                    _provider.CurrentScope = Parent;
                    break;
                }
            }
        }

        public Dictionary<string, object> GetLogProperties(Dictionary<string, object> properties = null)
        {
            properties = Parent?.GetLogProperties(properties);
            properties = properties ?? new Dictionary<string, object>();
            
            if (_state != null)
            {
                if (_state is IEnumerable<KeyValuePair<string, object>> dictionary)
                {
                    foreach (var pair in dictionary)
                    {
                        var key = pair.Key;
                        if (key != "Scope")
                        {
                            properties[key] = pair.Value;
                        }
                        else
                        {
                            var scopes = GetScopeProperty(properties);
                            scopes.Add(pair.Value);
                        }
                    }
                }
                else
                {
                    var scopes = GetScopeProperty(properties);
                    scopes.Add(_state);
                }
            }
            return properties;
        }

        private static List<object> GetScopeProperty(Dictionary<string, object> properties)
        {
            var scopes = properties.TryGetValue("Scope") as List<object>;
            if (scopes == null)
            {
                scopes = new List<object>();
                properties["Scope"] = scopes;
            }

            return scopes;
        }
    }
}
