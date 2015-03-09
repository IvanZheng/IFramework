using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public string Id { get; set; }
        protected Task _task;
        protected object _mutex = new object();
        protected Semaphore _semaphore = new Semaphore(0, 1);
        protected CancellationTokenSource _cancellationTokenSource;

        protected volatile bool _toExit = false;
        protected volatile bool _suspend = false;
        protected volatile bool _canceled = false;
        protected WorkDelegate _workDelegate;

        protected int _workInterval = 0;
        public int WorkInterval
        {
            get { return _workInterval; }
            set { _workInterval = value; }
        }

        protected void Sleep(int timeout)
        {
            Thread.Sleep(timeout);
        }

        public delegate void WorkDelegate();

        protected virtual void RunPrepare()
        {

        }

        protected virtual void RunCompleted()
        {

        }

        protected virtual void Run()
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
                catch (System.Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            RunCompleted();
        }

        protected virtual void Work()
        {

        }


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
                catch (Exception)
                {

                }
            }
        }

        public TaskWorker(string id = null)
        {
            Id = id;
        }

        public TaskWorker(WorkDelegate run, string id = null)
        : this(id)
        {
            _workDelegate = run;
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
                    _task = Task.Factory.StartNew(Run, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                else
                {
                    throw new InvalidOperationException("can not start when task is " + Status.ToString());
                }
            }
            return this;
        }

        public virtual void Wait(int millionSecondsTimeout = 0)
        {
            if (millionSecondsTimeout > 0)
            {
                Task.WaitAll(new Task[]{_task}, millionSecondsTimeout);
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
    }
}
