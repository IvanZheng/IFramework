using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IFramework.Logging.Abstracts
{
    public abstract class LoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private readonly AsyncLocal<LoggerScope> _value = new AsyncLocal<LoggerScope>();
        private readonly BlockingCollection<LogEvent> _logQueue = new BlockingCollection<LogEvent>();
        protected CancellationTokenSource CancellationTokenSource;
        protected bool Disposed = false;
        private Task _processLogTask;
        public bool AsyncLog { get; }
        protected LoggerProvider(LogLevel minLevel = LogLevel.Debug, bool asyncLog = true)
        {
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
                    var logEvent = _logQueue.Take(cs.Token); 
                    Log(logEvent);
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
                         Task.Delay(1000).Wait();
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

        internal void ProcessLog(LogEvent logEvent)
        {
            if (Disposed)
            {
                return;
            }
            if (AsyncLog)
            {
                _logQueue.Add(logEvent);
            }
            else
            {
                Log(logEvent);
            }
        }

        protected abstract void Log(LogEvent logEvent);
    }
}
