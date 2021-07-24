using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Aliyun.Api.LogService;
using Aliyun.Api.LogService.Domain.Log;
using Aliyun.Api.LogService.Infrastructure.Protocol.Http;
using IFramework.Infrastructure;
using IFramework.Logging.Abstracts;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.AliyunLog
{
    public class AliyunLoggerProvider :LoggerProvider
    {
        private readonly Func<LogEvent, LogGroupInfo> _getLogGroupInfo;
        private readonly HttpLogServiceClient _client;
        public AliyunLoggerProvider(AliyunLogOptions options, LogLevel minLevel = LogLevel.Information, bool asyncLog = true, Func<LogEvent, LogGroupInfo> getLogGroupInfo = null)
            :base(minLevel, asyncLog)
        {
            _getLogGroupInfo = getLogGroupInfo ?? GetLogGroupInfo;
            Options = options;

            _client = LogServiceClientBuilders.HttpBuilder
                                           .Endpoint(Options.Endpoint, Options.Project)
                                           .Credential(Options.AccessKeyId, Options.AccessKey)
                                           .Build();
        }
        public AliyunLogOptions Options { get; }
        protected override ILogger CreateLoggerImplement(LoggerProvider provider, string categoryName, LogLevel minLevel)
        {
            return new DefaultLogger(this, categoryName, minLevel);
        }

        private LogGroupInfo GetLogGroupInfo(LogEvent logEvent)
        {
            return new LogGroupInfo
            {
                Logs = new List<LogInfo>{new LogInfo
                {
                    Time = logEvent.Timestamp,
                    Contents = logEvent.GetContents()
                }},
                Topic = logEvent.Logger,
                Source = Utility.GetLocalIpv4().ToString()
            };
        }

        protected override void Log(LogEvent logEvent)
        {
            if (Disposed)
            {
                return;
            }

            var logInfo = _getLogGroupInfo(logEvent);
            var response =  _client.PostLogStoreLogsAsync(new PostLogsRequest(Options.LogStore,
                                                                              logInfo))
                                   .GetAwaiter()
                                   .GetResult();
            if (!response.IsSuccess && !Disposed)
            {
                Trace.WriteLine($"log event failed {response.Error.ErrorCode} {response.Error.ErrorMessage}");
            }
        }
    }
}
