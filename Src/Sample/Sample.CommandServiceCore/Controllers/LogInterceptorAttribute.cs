using System;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.CommandServiceCore.Controllers
{
    public class LogInterceptorAttribute : InterceptorAttribute
    {
        protected ILogger GetLogger(Type type)
        {
            return ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(type);
        }

        protected ILogger GetLogger<T>()
        {
            return ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<T>();
        }

        public override async Task<object> ProcessAsync(Func<Task<object>> funcAsync,
                                                        Type targetType,
                                                        object invocationTarget,
                                                        MethodInfo method,
                                                        MethodInfo methodInvocationTarget)
        {
            var logger = GetLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = await funcAsync().ConfigureAwait(false);
            logger.LogDebug($"{method.Name} leave");
            return result;
        }

        public override object Process(Func<object> func,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget)
        {
            var logger = GetLogger(targetType);
            logger.LogDebug($"{method.Name} enter");
            var result = func();
            logger.LogDebug($"{method.Name} leave");
            return result;
        }
    }
}