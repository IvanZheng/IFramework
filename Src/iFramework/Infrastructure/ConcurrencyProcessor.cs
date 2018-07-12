using System;
using System.Data;
using System.Threading.Tasks;
using IFramework.Exceptions;

namespace IFramework.Infrastructure
{
    public class ConcurrencyProcessor : IConcurrencyProcessor
    {
        protected virtual string UnKnownMessage { get; set; } = ErrorCode.UnknownError.ToString();

        public virtual async Task<T> ProcessAsync<T>(Func<Task<T>> func,
                                                     int retryCount = 50,
                                                     bool continueOnCapturedContext = false)
        {
            do
            {
                try
                {
                    return await func().ConfigureAwait(continueOnCapturedContext);
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual async Task ProcessAsync(Func<Task> func,
                                               int retryCount = 50,
                                               bool continueOnCapturedContext = false)
        {
            do
            {
                try
                {
                    await func().ConfigureAwait(continueOnCapturedContext);
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual void Process(Action action,
                                    int retryCount = 50)
        {
            do
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual T Process<T>(Func<T> func,
                                    int retryCount = 50)
        {
            do
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }
    }
}