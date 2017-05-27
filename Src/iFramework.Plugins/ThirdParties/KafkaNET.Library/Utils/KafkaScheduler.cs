using System;
using System.Threading;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace Kafka.Client.Utils
{
    /// <summary>
    ///     A scheduler for running jobs in the background
    /// </summary>
    internal class KafkaScheduler : IDisposable
    {
        public delegate void KafkaSchedulerDelegate();

        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(KafkaScheduler));
        private readonly object shuttingDownLock = new object();
        private volatile bool disposed;
        private KafkaSchedulerDelegate methodToRun;

        private Timer timer;

        public void Dispose()
        {
            if (disposed)
                return;

            lock (shuttingDownLock)
            {
                if (disposed)
                    return;

                disposed = true;
            }

            try
            {
                if (timer != null)
                {
                    timer.Dispose();
                    Logger.Info("shutdown scheduler");
                }
            }
            catch (Exception exc)
            {
                Logger.WarnFormat("Ignoring unexpected errors on closing", exc.FormatException());
            }
        }

        public void ScheduleWithRate(KafkaSchedulerDelegate method, long delayMs, long periodMs)
        {
            methodToRun = method;
            TimerCallback tcb = HandleCallback;
            timer = new Timer(tcb, null, delayMs, periodMs);
        }

        private void HandleCallback(object o)
        {
            methodToRun();
        }
    }
}