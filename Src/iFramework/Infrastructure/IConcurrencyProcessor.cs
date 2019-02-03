using System;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public interface IConcurrencyProcessor
    {
        Task<T> ProcessAsync<T>(Func<Task<T>> func, string[] uniqueConstrainNames = null, int retryCount = 50);

        Task ProcessAsync(Func<Task> func, string[] uniqueConstrainNames = null,int retryCount = 50);

        void Process(Action action, string[] uniqueConstrainNames = null,int retryCount = 50);

        T Process<T>(Func<T> func, string[] uniqueConstrainNames = null, int retryCount = 50);
    }
}