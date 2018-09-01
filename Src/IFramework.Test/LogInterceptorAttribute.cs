using System;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFramework.Test
{
    public class LogInterceptorAttribute : InterceptorAttribute
    {
        public override async Task<T> ProcessAsync<T>(Func<Task<T>> funcAsync,
                                                      IObjectProvider objectProvider,
                                                      Type targetType,
                                                      object invocationTarget,
                                                      MethodInfo method,
                                                      MethodInfo methodInvocationTarget,
                                                      object[] arguments)
        {
            var logger = objectProvider.GetService<ILoggerFactory>().CreateLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = await funcAsync().ConfigureAwait(false);
            logger.LogDebug($"{method.Name} leave");
            return result;
        }

        public override async Task ProcessAsync(Func<Task> funcAsync,
                                                IObjectProvider objectProvider,
                                                Type targetType,
                                                object invocationTarget,
                                                MethodInfo method,
                                                MethodInfo methodInvocationTarget,
                                                object[] arguments)
        {
            var logger = objectProvider.GetService<ILoggerFactory>().CreateLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            await funcAsync().ConfigureAwait(false);
            logger.LogDebug($"{method.Name} leave");
        }

        public override object Process(Func<object> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget,
                                       object[] arguments)
        {
            var logger = objectProvider.GetService<ILoggerFactory>().CreateLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = func();
            logger.LogDebug($"{method.Name} leave");
            return result;
        }
    }
}