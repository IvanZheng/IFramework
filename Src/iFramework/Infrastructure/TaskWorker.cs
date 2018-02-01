using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure
{
    public enum WorkerStatus
    {
        NotStarted,
        Running,
        Suspended,
        Completed,
        Canceled
    }

    public class TaskWorker
    {
        public delegate void WorkDelegate();

        private readonly ILogger _logger;
        private readonly WorkDelegate _workDelegate;

        protected volatile bool Canceled;
        protected CancellationTokenSource CancellationTokenSource;
        protected object Mutex = new object();
        protected Semaphore Semaphore = new Semaphore(0, 1);
        protected volatile bool Suspended;
        protected Task Task;

        protected volatile bool ToExit;

        public TaskWorker(string id = null)
        {
            Id = id;
            _logger = IoCFactory.Resolve<ILoggerFactory>().CreateLogger(GetType());
        }

        public TaskWorker(WorkDelegate run, string id = null)
            : this(id)
        {
            _workDelegate = run;
        }

        public string Id { get; set; }

        public int WorkInterval { get; set; }

        public WorkerStatus Status
        {
            get
            {
                WorkerStatus status;
                if (Canceled)
                {
                    status = WorkerStatus.Canceled;
                }
                else if (Task == null)
                {
                    status = WorkerStatus.NotStarted;
                }
                else if (Suspended)
                {
                    status = WorkerStatus.Suspended;
                }
                else if (Task.Status == TaskStatus.RanToCompletion)
                {
                    status = WorkerStatus.Completed;
                }
                else
                {
                    status = WorkerStatus.Running;
                }
                return status;
            }
        }


        protected void Sleep(int timeout)
        {
            Thread.Sleep(timeout);
        }


        protected virtual void RunPrepare() { }

        protected virtual void RunCompleted() { }

        protected virtual void Run()
        {
            try
            {
                RunPrepare();
                while (!ToExit)
                {
                    try
                    {
                        CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (Suspended)
                        {
                            Semaphore.WaitOne();
                            Suspended = false;
                        }
                        CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (_workDelegate != null)
                        {
                            _workDelegate.Invoke();
                        }
                        else
                        {
                            Work();
                        }
                        CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (WorkInterval > 0)
                        {
                            Sleep(WorkInterval);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
                RunCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TaskWork run failed!");
            }
        }

        protected virtual void Work() { }


        public virtual void Suspend()
        {
            Suspended = true;
        }

        public virtual void Resume()
        {
            lock (Mutex)
            {
                try
                {
                    Semaphore.Release();
                }
                catch (Exception) { }
            }
        }

        public virtual TaskWorker Start()
        {
            lock (Mutex)
            {
                if (Status == WorkerStatus.Canceled
                    || Status == WorkerStatus.Completed
                    || Status == WorkerStatus.NotStarted)
                {
                    Canceled = false;
                    Suspended = false;
                    ToExit = false;
                    CancellationTokenSource = new CancellationTokenSource();
                    Task = Task.Factory.StartNew(Run, CancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                                                 TaskScheduler.Default);
                }
                else
                {
                    throw new InvalidOperationException("can not start when task is " + Status);
                }
            }
            return this;
        }

        public virtual void Wait(int millionSecondsTimeout = 0)
        {
            if (millionSecondsTimeout > 0)
            {
                Task.WaitAll(new[] {Task}, millionSecondsTimeout);
            }
            else
            {
                Task.WaitAll(Task);
            }
        }

        protected virtual void Complete()
        {
            ToExit = true;
        }

        public virtual void Stop(bool forcibly = false)
        {
            lock (Mutex)
            {
                if (!ToExit)
                {
                    ToExit = true;
                    if (Suspended)
                    {
                        Resume();
                    }
                    if (forcibly)
                    {
                        CancellationTokenSource.Cancel(true);
                    }
                    Canceled = true;
                    Task = null;
                }
            }
        }
    }
}