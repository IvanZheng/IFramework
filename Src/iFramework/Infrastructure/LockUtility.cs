using System;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public static class LockUtility
    {
        public static async Task LockAsync(this ILockProvider lockProvider,
                                           string name,
                                           Func<Task> func,
                                           TimeSpan timeout,
                                           CancellationToken? cancellationToken = null,
                                           bool continueOnCapturedContext = false)
        {
            var @lock = await lockProvider.AcquireAsync(name, timeout, cancellationToken ?? CancellationToken.None)
                                          .ConfigureAwait(continueOnCapturedContext);
            try
            {
                await func().ConfigureAwait(continueOnCapturedContext);
            }
            finally
            {
                await @lock.ReleaseAsync()
                           .ConfigureAwait(continueOnCapturedContext);
            }
        }

        public static async Task<TResult> LockAsync<TResult>(this ILockProvider lockProvider,
                                                             string name,
                                                             Func<Task<TResult>> func,
                                                             TimeSpan timeout,
                                                             CancellationToken cancellationToken,
                                                             bool continueOnCapturedContext = false)
        {
            TResult result;
            var @lock = await lockProvider.AcquireAsync(name, timeout, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext);
            try
            {
                result = await func().ConfigureAwait(continueOnCapturedContext);
            }
            finally
            {
                await @lock.ReleaseAsync()
                           .ConfigureAwait(continueOnCapturedContext);
            }
            return result;
        }


        public static async Task LockAsync(this ILockProvider lockProvider,
                                           string name,
                                           Action action,
                                           TimeSpan timeout,
                                           CancellationToken? cancellationToken = null,
                                           bool continueOnCapturedContext = false)
        {
            var @lock = await lockProvider.AcquireAsync(name, timeout, cancellationToken ?? CancellationToken.None)
                                          .ConfigureAwait(continueOnCapturedContext);
            try
            {
                action();
            }
            finally
            {
                await @lock.ReleaseAsync()
                           .ConfigureAwait(continueOnCapturedContext);
            }
        }

        public static async Task<TResult> LockAsync<TResult>(this ILockProvider lockProvider,
                                                             string name,
                                                             Func<TResult> func,
                                                             TimeSpan timeout,
                                                             CancellationToken cancellationToken,
                                                             bool continueOnCapturedContext = false)
        {
            TResult result;
            var @lock = await lockProvider.AcquireAsync(name, timeout, cancellationToken)
                                          .ConfigureAwait(continueOnCapturedContext);
            try
            {
                result = func();
            }
            finally
            {
                await @lock.ReleaseAsync()
                           .ConfigureAwait(continueOnCapturedContext);
            }
            return result;
        }
    }
}