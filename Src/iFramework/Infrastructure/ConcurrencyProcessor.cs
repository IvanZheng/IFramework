using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Exceptions;

namespace IFramework.Infrastructure
{
    public class ConcurrencyProcessor : IConcurrencyProcessor
    {
        protected virtual string UnKnownMessage { get; set; } = ErrorCode.UnknownError.ToString();

        public virtual async Task<T> ProcessAsync<T>(Func<Task<T>> func,
                                                     string[] uniqueConstrainNames = null,
                                                     int retryCount = 50)
        {
            do
            {
                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException || NeedRetryDueToUniqueConstrainException(ex, uniqueConstrainNames))
                        || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual async Task ProcessAsync(Func<Task> func,
                                               string[] uniqueConstrainNames = null,
                                               int retryCount = 50)
        {
            do
            {
                try
                {
                    await func().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException || NeedRetryDueToUniqueConstrainException(ex, uniqueConstrainNames)) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual void Process(Action action,
                                    string[] uniqueConstrainNames = null,
                                    int retryCount = 50)
        {
            do
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    if (!(ex is DBConcurrencyException || NeedRetryDueToUniqueConstrainException(ex, uniqueConstrainNames)) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        public virtual T Process<T>(Func<T> func,
                                    string[] uniqueConstrainNames = null,
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
                    if (!(ex is DBConcurrencyException || NeedRetryDueToUniqueConstrainException(ex, uniqueConstrainNames)) || retryCount-- <= 0)
                    {
                        throw;
                    }
                }
            } while (true);
        }

        private bool NeedRetryDueToUniqueConstrainException(Exception exception, string[] uniqueConstrainNames)
        {
            var needRetry = false;
            if (uniqueConstrainNames?.Length > 0 && exception.GetBaseException() is DbException dbException)
            {
                var number = dbException.GetPropertyValue<int>("Number");
                needRetry = (dbException.Source.Contains("MySql") && number == 1062 ||
                             dbException.Source.Contains("SqlClient") && (number == 2601 || number == 2627 || number == 547)) &&
                            uniqueConstrainNames.Any(dbException.Message.Contains);
            }

            return needRetry;
        }
    }
}