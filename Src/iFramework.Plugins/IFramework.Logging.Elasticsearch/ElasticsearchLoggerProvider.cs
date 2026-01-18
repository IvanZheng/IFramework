using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Logging.Abstracts;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Elasticsearch
{
    public class ElasticsearchLoggerProvider:LoggerProvider
    {
        private readonly ElasticsearchLogOptions _options;
        private readonly Lazy<IElasticSearchService> _elasticSearchService = new Lazy<IElasticSearchService>(() =>  IFramework.DependencyInjection.ObjectProviderFactory.GetService<IElasticSearchService>());
        public ElasticsearchLoggerProvider(ElasticsearchLogOptions options,
                                           LogLevel minLevel = LogLevel.Debug,
                                           bool asyncLog = true) 
            : base(minLevel, asyncLog)
        {
            _options = options;
        }

        protected override ILogger CreateLoggerImplement(LoggerProvider provider, string categoryName, LogLevel minLevel)
        {
            return new DefaultLogger(this, categoryName, minLevel);
        }

        public override void ProcessLog(LogEvent logEvent)
        {
            if (!logEvent.Logger.Contains(nameof(IElasticSearchService)))
            {
                base.ProcessLog(logEvent);
            }
        }

        protected override void Log(params LogEvent[] logEvents)
        {
            var logs = logEvents.Select(GetContent)
                                .ToArray();
            if (logs.Length > 0)
            {
                _elasticSearchService.Value.AddLog(logs)
                                     .GetAwaiter().GetResult();
            }
            
        }

        public Dictionary<string, object> GetContent(LogEvent logEvent)
        {
             var data = new Dictionary<string, object>()
             {
                 ["Index"] = _options.Index,
                 ["Message"] = logEvent.State,
                 ["Level"] = logEvent.Level.ToString(),
                 ["Logger"] = logEvent.Logger,
                 ["Time"] = logEvent.Timestamp,
                 ["App"] = _options.App?.ToLower(),
                 ["Env"] = _options.Env,
                 ["Source"] = Utility.GetLocalIpv4().ToString(),
                 ["Scope"] = logEvent.Scope
             };
             if (logEvent.Exception != null)
             {
                 data["Exception"] = logEvent.Exception.Message;
                 data["StackTrace"] = logEvent.Exception.StackTrace;
             }

             return data;
        }
        private string FormatState(object state) => state is string || state.GetType().IsPrimitive || state is Guid ? state.ToString() : state.ToJson();
    }
}
