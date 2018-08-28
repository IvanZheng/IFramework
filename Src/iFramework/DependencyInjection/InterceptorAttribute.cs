using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection
{
    public abstract class InterceptorAttribute : Attribute
    {
        /// <summary>
        ///     the larger, the earlier processing
        /// </summary>
        public int Order { get; set; }

        public abstract Task ProcessAsync(Func<Task> funcAsync,
                                          IObjectProvider objectProvider,
                                          Type targetType,
                                          object invocationTarget,
                                          MethodInfo method,
                                          MethodInfo methodInvocationTarget);

        public abstract Task<T> ProcessAsync<T>(Func<Task<T>> funcAsync,
                                                IObjectProvider objectProvider,
                                                Type targetType,
                                                object invocationTarget,
                                                MethodInfo method,
                                                MethodInfo methodInvocationTarget);

        public abstract object Process(Func<dynamic> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget);
    }
}