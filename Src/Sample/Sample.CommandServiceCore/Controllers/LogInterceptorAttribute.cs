using System;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.CommandServiceCore.Controllers
{
    public class LogInterceptorAttribute : InterceptorAttribute
    {
        public override async Task<object> ProcessAsync(Func<Task<object>> funcAsync,
                                                        IObjectProvider objectProvider,
                                                        Type targetType,
                                                        object invocationTarget,
                                                        MethodInfo method,
                                                        MethodInfo methodInvocationTarget)
        {
            var logger = objectProvider.GetService<ILoggerFactory>().CreateLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = await funcAsync().ConfigureAwait(false);
            logger.LogDebug($"{method.Name} leave");
            return result;
        }

        public override object Process(Func<object> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget)
        {
            var logger = objectProvider.GetService<ILoggerFactory>().CreateLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = func();
            logger.LogDebug($"{method.Name} leave");
            return result;
        }
    }
}