using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.DependencyInjection;

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

        protected volatile bool _canceled;
        protected CancellationTokenSource _cancellationTokenSource;
        protected object _mutex = new object();
        protected Semaphore _semaphore = new Semaphore(0, 1);
        protected volatile bool _suspend;
        protected Task _task;

        protected volatile bool _toExit;
        protected WorkDelegate _workDelegate;

        protected int _workInterval;

        public TaskWorker(string id = null)
        {
            Id = id;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType());
        }

        public TaskWorker(WorkDelegate run, string id = null)
            : this(id)
        {
            _workDelegate = run;
        }

        public string Id { get; set; }

        public int WorkInterval
        {
            get => _workInterval;
            set => _workInterval = value;
        }

        public WorkerStatus Status
        {
            get
            {
                WorkerStatus status;
                if (_canceled)
                {
                    status = WorkerStatus.Canceled;
                }
                else if (_task == null)
                {
                    status = WorkerStatus.NotStarted;
                }
                else if (_suspend)
                {
                    status = WorkerStatus.Suspended;
                }
                else if (_task.Status == TaskStatus.RanToCompletion)
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
                while (!_toExit)
                {
                    try
                    {
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (_suspend)
                        {
                            _semaphore.WaitOne();
                            _suspend = false;
                        }
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        if (_workDelegate != null)
                        {
                            _workDelegate.Invoke();
                        }
                        else
                        {
                            Work();
                        }
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
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
                _logger?.Error(ex);
                //System.IO.File.AppendAllText(System.AppDomain.CurrentDomain.BaseDirectory + "/log/taskError.txt",
                //    ex.InnerException.Message + "\r\n" + ex.InnerException.StackTrace);
            }
        }

        protected virtual void Work() { }


        public virtual void Suspend()
        {
            _suspend = true;
        }

        public virtual void Resume()
        {
            lock (_mutex)
            {
                try
                {
                    _semaphore.Release();
                }
                catch (Exception) { }
            }
        }

        public virtual TaskWorker Start()
        {
            lock (_mutex)
            {
                if (Status == WorkerStatus.Canceled
                    || Status == WorkerStatus.Completed
                    || Status == WorkerStatus.NotStarted)
                {
                    _canceled = false;
                    _suspend = false;
                    _toExit = false;
                    _cancellationTokenSource = new CancellationTokenSource();
                    _task = Task.Factory.StartNew(Run, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning,
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
                Task.WaitAll(new[] {_task}, millionSecondsTimeout);
            }
            else
            {
                Task.WaitAll(_task);
            }
        }

        protected virtual void Complete()
        {
            _toExit = true;
        }

        public virtual void Stop(bool forcibly = false)
        {
            lock (_mutex)
            {
                if (!_toExit)
                {
                    _toExit = true;
                    if (_suspend)
                    {
                        Resume();
                    }
                    if (forcibly)
                    {
                        _cancellationTokenSource.Cancel(true);
                    }
                    _canceled = true;
                    _task = null;
                }
            }
        }
    }
}