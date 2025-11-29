using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Abstracts
{
    public abstract class LoggerProvider : ILoggerProvider
    {
        private readonly int _batchCount;
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private readonly AsyncLocal<LoggerScope> _value = new AsyncLocal<LoggerScope>();
        private readonly ConcurrentQueue<LogEvent> _logQueue = new ConcurrentQueue<LogEvent>();
        protected CancellationTokenSource CancellationTokenSource;
        protected bool Disposed = false;
        private Task _processLogTask;
        public bool AsyncLog { get; }
        protected LoggerProvider(LogLevel minLevel = LogLevel.Debug, bool asyncLog = true, int batchCount = 100)
        {
            _batchCount = batchCount;
            MinLevel = minLevel;
            AsyncLog = asyncLog;
            if (asyncLog)
            {
                CancellationTokenSource = new CancellationTokenSource();
                _processLogTask = Task.Factory.StartNew(cs => ProcessLogs(cs as CancellationTokenSource),
                                                        CancellationTokenSource,
                                                        CancellationTokenSource.Token,
                                                        TaskCreationOptions.LongRunning,
                                                        TaskScheduler.Default);
            }
        }

        private void ProcessLogs(CancellationTokenSource cs)
        {
            while (!cs.IsCancellationRequested)
            {
                try
                {
                    var logEvents = new List<LogEvent>();
                    while (logEvents.Count < _batchCount && _logQueue.TryDequeue(out var logEvent))
                    {
                        logEvents.Add(logEvent);
                    }
                   
                    if (logEvents.Count == 0)
                    {
                        Task.Delay(100, CancellationToken.None).Wait();
                    }
                    else
                    {
                        Log(logEvents.ToArray());
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    if (!cs.IsCancellationRequested)
                    {
                         Task.Delay(1000, CancellationToken.None).Wait();
                    }
                }
            }
        }

        public LogLevel MinLevel { get; }

        internal LoggerScope CurrentScope
        {
            get => this._value.Value;
            set => this._value.Value = value;
        }


        public IDisposable BeginScope<T>(T state)
        {
            return new LoggerScope(this, state);
        }

        public virtual void Dispose()
        {
            if (CancellationTokenSource != null && !Disposed)
            {
                Disposed = true;
                CancellationTokenSource.Cancel(true);
                CancellationTokenSource = null;
                _processLogTask.Wait();
                _processLogTask.Dispose();
                _processLogTask = null;
            }
        }

        public virtual ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, key => CreateLoggerImplement(this, key, MinLevel));
        }

        protected abstract ILogger CreateLoggerImplement(LoggerProvider provider, string categoryName, LogLevel minLevel);

        public virtual void ProcessLog(LogEvent logEvent)
        {
            if (Disposed)
            {
                return;
            }
            if (AsyncLog)
            {
                _logQueue.Enqueue(logEvent);
            }
            else
            {
                Log(logEvent);
            }
        }

        protected abstract void Log(params LogEvent[] logEvents);
    }
}
