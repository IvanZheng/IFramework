using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using IFramework.Infrastructure;
using MethodInfo = System.Reflection.MethodInfo;

namespace IFramework.DependencyInjection.Autofac
{
    public abstract class InterceptorBase : IInterceptor
    {
        protected readonly IObjectProvider ObjectProvider;

        protected InterceptorBase(IObjectProvider objectProvider)
        {
            ObjectProvider = objectProvider;
        }

        public virtual void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }

        protected virtual object Process(IInvocation invocation)
        {
            invocation.Proceed();
            return invocation.ReturnValue;
        }

        protected Type GetTaskResultType(IInvocation invocation)
        {
            return invocation.Method.ReturnType.GetGenericArguments().FirstOrDefault();
        }

        protected static Task<T> ProcessTaskAsync<T>(IInvocation invocation)
        {
            invocation.Proceed();
            return invocation.ReturnValue as Task<T>;
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
        public DefaultInterceptor(IObjectProvider objectProvider) : base(objectProvider) { }


        public virtual void InterceptAsync<T>(IInvocation invocation, InterceptorAttribute[] interceptorAttributes)
        {
            using (var semaphore = new Semaphore(1, 1))
            {
                semaphore.WaitOne();
                Func<Task<T>> processAsyncFunc = () =>
                {
                    invocation.Proceed();
                    var task = invocation.ReturnValue as Task<T>;
                    semaphore.Release();
                    return task;
                };

                foreach (var interceptor in interceptorAttributes)
                {
                    var func = processAsyncFunc;
                    processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                      ObjectProvider,
                                                                      invocation.TargetType,
                                                                      invocation.InvocationTarget,
                                                                      invocation.Method,
                                                                      invocation.Arguments);
                }

                var returnValue = processAsyncFunc();

                semaphore.WaitOne();
                invocation.ReturnValue = returnValue;
                semaphore.Release();
            }
        }

        public virtual void InterceptAsync(IInvocation invocation, InterceptorAttribute[] interceptorAttributes)
        {
            using (var semaphore = new Semaphore(1, 1))
            {
                semaphore.WaitOne();
                Func<Task> processAsyncFunc = () =>
                {
                    invocation.Proceed();
                    var task = invocation.ReturnValue as Task;
                    semaphore.Release();
                    return task;
                };
                foreach (var interceptor in interceptorAttributes)
                {
                    var func = processAsyncFunc;
                    processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                      ObjectProvider,
                                                                      invocation.TargetType,
                                                                      invocation.InvocationTarget,
                                                                      invocation.Method,
                                                                      invocation.Arguments);
                }

                var returnValue = processAsyncFunc();
                semaphore.WaitOne();
                invocation.ReturnValue = returnValue;
                semaphore.Release();
            }
        }

        public override void Intercept(IInvocation invocation)
        {
            var isTaskResult = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
            var interceptorAttributes = GetInterceptorAttributes(invocation);

            if (interceptorAttributes.Length > 0)
            {
                if (isTaskResult)
                {
                    var resultType = GetTaskResultType(invocation);
                    if (resultType == null)
                    {
                        InterceptAsync(invocation, interceptorAttributes);
                    }
                    else
                    {
                        this.InvokeGenericMethod(nameof(InterceptAsync),
                                                 new object[] {invocation, interceptorAttributes},
                                                 resultType);
                    }
                }
                else
                {
                    if (invocation.Method.ReturnType != typeof(void))
                    {
                        Func<dynamic> processFunc = () => Process(invocation);
                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    ObjectProvider,
                                                                    invocation.TargetType,
                                                                    invocation.InvocationTarget,
                                                                    invocation.Method,
                                                                    invocation.Arguments);
                        }

                        invocation.ReturnValue = processFunc();
                    }
                    else
                    {
                        Action processFunc = () => Process(invocation);
                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    ObjectProvider,
                                                                    invocation.TargetType,
                                                                    invocation.InvocationTarget,
                                                                    invocation.Method,
                                                                    invocation.Arguments);
                        }

                        processFunc();
                    }
                }
            }
            else
            {
                base.Intercept(invocation);
            }
        }
    }
}