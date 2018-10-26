using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public interface IConcurrencyProcessor
    {
        Task<T> ProcessAsync<T>(Func<Task<T>> func,
                                int retryCount = 50,
                                bool continueOnCapturedContext = false);

        Task ProcessAsync(Func<Task> func,
                          int retryCount = 50,
                          bool continueOnCapturedContext = false);

        void Process(Action action,
                          int retryCount = 50);

        T Process<T>(Func<T> func,
                                int retryCount = 50);
    }
}