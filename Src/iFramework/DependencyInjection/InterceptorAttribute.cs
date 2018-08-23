using System;
using System.Reflection;
using System.Threading.Tasks;

namespace IFramework.DependencyInjection
{
    public abstract class InterceptorAttribute : Attribute
    {
        /// <summary>
        ///  the larger, the earlier processing
        /// </summary>
        public int Order { get; set; }
        public abstract Task<object> ProcessAsync(Func<Task<object>> funcAsync,
                                                  Type targetType,
                                                  object invocationTarget,
                                                  MethodInfo method,
                                                  MethodInfo methodInvocationTarget);

        public abstract object Process(Func<object> func,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget);
    }
}