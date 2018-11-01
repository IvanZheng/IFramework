using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Unity.Interception.InterceptionBehaviors;
using Unity.Interception.PolicyInjection.Pipeline;
using MethodInfo = System.Reflection.MethodInfo;

namespace IFramework.DependencyInjection.Unity
{
    public abstract class InterceptorBase : IInterceptionBehavior
    {
        protected readonly IObjectProvider ObjectProvider;

        protected InterceptorBase(IObjectProvider objectProvider)
        {
            ObjectProvider = objectProvider;
        }

        protected virtual IMethodReturn Process(IMethodInvocation invocation, GetNextInterceptionBehaviorDelegate getNext)
        {
            return getNext()(invocation, getNext); //在这里执行方法
        }

        protected Type GetTaskResultType(IMethodInvocation invocation)
        {
            return (invocation.MethodBase as MethodInfo)?.ReturnType.GetGenericArguments().FirstOrDefault();
        }

        protected static Task<T> ProcessTaskAsync<T>(IMethodInvocation invocation, GetNextInterceptionBehaviorDelegate getNext)
        {
            return (Task<T>) getNext()(invocation, getNext).ReturnValue;
        }

        protected static TAttribute GetMethodAttribute<TAttribute>(IMethodInvocation invocation, TAttribute defaultValue = null)
            where TAttribute : Attribute
        {
            var method = invocation.MethodBase as MethodInfo;
            return method.GetCustomAttribute<TAttribute>() ??
                   method?.DeclaringType?.GetCustomAttribute<TAttribute>() ??
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

        protected static InterceptorAttribute[] GetInterceptorAttributes(IMethodInvocation invocation)
        {
            var methodInfo = invocation.MethodBase as MethodInfo;
            if (methodInfo == null)
            {
                return null;
            }
            return GetInterceptorAttributes(methodInfo)
                .Union(GetInterceptorAttributes(methodInfo.DeclaringType))
                .OrderBy(i => i.Order)
                .ToArray();
        }

        public virtual IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            return getNext()(input, getNext); //在这里执行方法
        }

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        public bool WillExecute => true;
    }

    public class DefaultInterceptor : InterceptorBase
    {
        public DefaultInterceptor(IObjectProvider objectProvider) : base(objectProvider) { }


        public virtual IMethodReturn InterceptAsync<T>(IMethodInvocation invocation, 
                                                       GetNextInterceptionBehaviorDelegate getNext,
                                                       InterceptorAttribute[] interceptorAttributes)
        {
            Func<Task<T>> processAsyncFunc = () =>
            {
                var methodReturn = getNext()(invocation, getNext);
                return methodReturn.ReturnValue as Task<T>;
            };
            foreach (var interceptor in interceptorAttributes)
            {
                var func = processAsyncFunc;
                processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                  ObjectProvider,
                                                                  invocation.Target.GetType(),
                                                                  invocation.Target,
                                                                  invocation.MethodBase as MethodInfo,
                                                                  invocation.Arguments.Cast<object>().ToArray());
            }
            var returnValue = processAsyncFunc();
            return invocation.CreateMethodReturn(returnValue);
        }

        public virtual IMethodReturn InterceptAsync(IMethodInvocation invocation, GetNextInterceptionBehaviorDelegate getNext, InterceptorAttribute[] interceptorAttributes)
        {
            Func<Task> processAsyncFunc = () =>
            {
                IMethodReturn  methodReturn = getNext()(invocation, getNext);
                return methodReturn.ReturnValue as Task;
            };

            foreach (var interceptor in interceptorAttributes)
            {
                var func = processAsyncFunc;
                processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                  ObjectProvider,
                                                                  invocation.Target.GetType(),
                                                                  invocation.Target,
                                                                  invocation.MethodBase as MethodInfo,
                                                                  invocation.Arguments.Cast<object>().ToArray());
            }
            var returnValue = processAsyncFunc();
            return invocation.CreateMethodReturn(returnValue);
        }

        public override IMethodReturn Invoke(IMethodInvocation invocation, GetNextInterceptionBehaviorDelegate getNext)
        {
            var method = invocation.MethodBase as MethodInfo;
            if (method == null)
            {
                throw new Exception($"{invocation.MethodBase} is not MethodInfo!");
            }
            var isTaskResult = typeof(Task).IsAssignableFrom(method.ReturnType);
            var interceptorAttributes = GetInterceptorAttributes(invocation);

            if (interceptorAttributes.Length > 0)
            {
                if (isTaskResult)
                {
                    var resultType = GetTaskResultType(invocation);
                    if (resultType == null)
                    {
                        return InterceptAsync(invocation, getNext, interceptorAttributes);
                    }
                    else
                    {
                        return this.InvokeGenericMethod(nameof(InterceptAsync),
                                                 new object[] {invocation, getNext, interceptorAttributes},
                                                 resultType) as IMethodReturn;
                    }
                }
                else
                {
                    if (method.ReturnType != typeof(void))
                    {
                        Func<dynamic> processFunc = () => Process(invocation, getNext).ReturnValue;

                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    ObjectProvider,
                                                                    invocation.Target.GetType(),
                                                                    invocation.Target,
                                                                    invocation.MethodBase as MethodInfo,
                                                                    invocation.Arguments.Cast<object>().ToArray());
                        }
                        var returnValue = processFunc();
                        return invocation.CreateMethodReturn(returnValue);
                    }
                    else
                    {
                        IMethodReturn methodReturn = null;
                        Action processFunc = () =>
                        {
                            methodReturn = Process(invocation, getNext);
                        };
                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    ObjectProvider,
                                                                    invocation.Target.GetType(),
                                                                    invocation.Target,
                                                                    invocation.MethodBase as MethodInfo,
                                                                    invocation.Arguments.Cast<object>().ToArray());
                        }
                        processFunc();
                        return methodReturn ?? invocation.CreateMethodReturn(null);
                    }
                }
            }
            else
            {
                return base.Invoke(invocation, getNext);
            }
        }
    }
}