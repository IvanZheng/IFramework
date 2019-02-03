using System;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection
{
    public class ConcurrentProcessAttribute : InterceptorAttribute
    {
        public readonly string[] UniqueConstrainNames;

        public ConcurrentProcessAttribute(string[] uniqueConstrainNames = null, int retryTimes = 50)
        {
            UniqueConstrainNames = uniqueConstrainNames;
            RetryTimes = retryTimes;
        }

        public int RetryTimes { get; set; }

        public override Task<T> ProcessAsync<T>(Func<Task<T>> funcAsync,
                                                IObjectProvider objectProvider,
                                                Type targetType,
                                                object invocationTarget,
                                                MethodInfo method,
                                                object[] arguments)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            return concurrencyProcessor.ProcessAsync(funcAsync, UniqueConstrainNames, RetryTimes);
        }

        public override Task ProcessAsync(Func<Task> funcAsync,
                                          IObjectProvider objectProvider,
                                          Type targetType,
                                          object invocationTarget,
                                          MethodInfo method,
                                          object[] arguments)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            return concurrencyProcessor.ProcessAsync(funcAsync, UniqueConstrainNames, RetryTimes);
        }

        public override object Process(Func<dynamic> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       object[] arguments)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            return concurrencyProcessor.Process(func, UniqueConstrainNames, RetryTimes);
        }

        public override void Process(Action func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       object[] arguments)
        {
            var concurrencyProcessor = objectProvider.GetService<IConcurrencyProcessor>();
            concurrencyProcessor.Process(func, UniqueConstrainNames, RetryTimes);
        }
    }
}