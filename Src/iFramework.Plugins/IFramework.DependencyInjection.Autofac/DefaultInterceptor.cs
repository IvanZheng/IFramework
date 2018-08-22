using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using IFramework.Infrastructure;
using Microsoft.Extensions.Logging;

namespace IFramework.DependencyInjection.Autofac
{
    public abstract class InterceptorBase : IInterceptor
    {
        private readonly ILoggerFactory _loggerFactory;

        public InterceptorBase(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public virtual void Intercept(IInvocation invocation)
        {
            var isTaskResult = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
            if (isTaskResult)
            {
                ProcessAsync(invocation);
            }
            else
            {
                Process(invocation);
            }
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
    }


    public class DefaultInterceptor : InterceptorBase
    {
        public DefaultInterceptor(ILoggerFactory loggerFactory) : base(loggerFactory) { }

        protected override object Process(IInvocation invocation)
        {
            var transactionAttribute = GetMethodAttribute<TransactionAttribute>(invocation);
            if (transactionAttribute != null)
            {
                TransactionExtension.DoInTransaction(invocation.Proceed, transactionAttribute.IsolationLevel, transactionAttribute.Scope);
            }
            else
            {
                base.Process(invocation);
            }

            return invocation.ReturnValue;
        }

        protected override async Task<object> ProcessAsync(IInvocation invocation)
        {
            object result = null;
            var transactionAttribute = GetMethodAttribute<TransactionAttribute>(invocation);
            if (transactionAttribute != null)
            {
                await TransactionExtension.DoInTransactionAsync(async () =>
                                                                {
                                                                    result = await base.ProcessAsync(invocation)
                                                                                       .ConfigureAwait(false);
                                                                },
                                                                transactionAttribute.IsolationLevel,
                                                                transactionAttribute.Scope)
                                          .ConfigureAwait(false);
            }
            else
            {
                result = await base.ProcessAsync(invocation).ConfigureAwait(false);
            }

            return result;
        }
    }
}