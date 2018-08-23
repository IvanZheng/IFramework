using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using IFramework.Infrastructure;
using Microsoft.Extensions.Logging;
using MethodInfo = System.Reflection.MethodInfo;

namespace IFramework.DependencyInjection.Autofac
{
    public abstract class InterceptorBase : IInterceptor
    {
        protected readonly ILoggerFactory LoggerFactory;

        protected InterceptorBase(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public virtual void Intercept(IInvocation invocation)
        {
            var isTaskResult = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
            invocation.ReturnValue = isTaskResult ? ProcessAsync(invocation) : Process(invocation);
        }

        protected virtual object Process(IInvocation invocation)
        {
            invocation.Proceed();
            return invocation.ReturnValue;
        }

        protected virtual Task<object> ProcessAsync(IInvocation invocation)
        {
            return ProcessTaskAsync(invocation);
        }

        protected static async Task<object> ProcessTaskAsync(IInvocation invocation)
        {
            dynamic result = null;
            var taskHasResult = invocation.Method.ReturnType.IsGenericType;
            invocation.Proceed();
            dynamic task = invocation.ReturnValue;
            if (taskHasResult)
            {
                result = await task.ConfigureAwait(false);
            }
            else
            {
                await task.ConfigureAwait(false);
            }

            return result;
        }

        protected static TAttribute GetMethodAttribute<TAttribute>(IInvocation invocation, TAttribute defaultValue = null)
            where TAttribute : Attribute
        {
            return invocation.Method.GetCustomAttribute<TAttribute>() ??
                   invocation.Method.DeclaringType?.GetCustomAttribute<TAttribute>() ??
                   invocation.MethodInvocationTarget?.GetCustomAttribute<TAttribute>() ??
                   invocation.MethodInvocationTarget?.DeclaringType?.GetCustomAttribute<TAttribute>() ??
                   defaultValue;
        }

        private static IEnumerable<InterceptorAttribute> GetInterceptorAttributes(MethodInfo methodInfo)
        {
            return methodInfo?.GetCustomAttributes(typeof(InterceptorAttribute), true).Cast<InterceptorAttribute>() ?? new InterceptorAttribute[0];
        }

        private static IEnumerable<InterceptorAttribute> GetInterceptorAttributes(Type type)
        {
            return type?.GetCustomAttributes(typeof(InterceptorAttribute), true).Cast<InterceptorAttribute>() ?? new InterceptorAttribute[0];
        }

        protected static InterceptorAttribute[] GetInterceptorAttributes(IInvocation invocation)
        {
            return GetInterceptorAttributes(invocation.Method)
                .Union(GetInterceptorAttributes(invocation.Method.DeclaringType))
                .Union(GetInterceptorAttributes(invocation.MethodInvocationTarget))
                .Union(GetInterceptorAttributes(invocation.MethodInvocationTarget?.DeclaringType))
                .OrderBy(i => i.Order)
                .ToArray();
        }
    }

    public class DefaultInterceptor : InterceptorBase
    {
        public DefaultInterceptor(ILoggerFactory loggerFactory) : base(loggerFactory) { }

        public override void Intercept(IInvocation invocation)
        {
            var isTaskResult = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
            var interceptorAttributes = GetInterceptorAttributes(invocation);
            if (interceptorAttributes.Length > 0)
            {
                if (isTaskResult)
                {
                    Func<Task<object>> processAsyncFunc = () => ProcessAsync(invocation);

                    foreach (var interceptor in interceptorAttributes)
                    {
                        var func = processAsyncFunc;
                        processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                          invocation.TargetType,
                                                                          invocation.InvocationTarget,
                                                                          invocation.Method,
                                                                          invocation.MethodInvocationTarget);
                    }
                    invocation.ReturnValue = processAsyncFunc();
                }
                else
                {
                    Func<object> processFunc = () => Process(invocation);
                    foreach (var interceptor in interceptorAttributes)
                    {
                        var func = processFunc;
                        processFunc = () => interceptor.Process(func,
                                                                invocation.TargetType,
                                                                invocation.InvocationTarget,
                                                                invocation.Method,
                                                                invocation.MethodInvocationTarget);
                    }
                    invocation.ReturnValue = processFunc();
                }
            }
            else
            {
                base.Intercept(invocation);
            }
        }
    }
}