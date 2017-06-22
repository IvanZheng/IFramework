using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using IFramework.Infrastructure.Logging;

namespace IFramework.Autofac
{
    public class AutofacLogInterceptor: IInterceptor
    {
        private readonly ILoggerFactory _loggerFactory;

        public AutofacLogInterceptor(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }


        public void Intercept(IInvocation invocation)
        {
            bool isFaulted = false;
            Exception exception = null;
            var targetType = invocation.TargetType;
            var targetTypeName = targetType.Assembly.IsDynamic ? targetType.BaseType?.Name : targetType.Name;

            var logger = _loggerFactory.Create(!string.IsNullOrWhiteSpace(targetTypeName) ? targetTypeName : targetType.Name);

            var parameters = invocation.Arguments
                                       .Select(a => (a ?? "").ToString())
                                       .ToList();

            logger?.DebugFormat("Enter method: {0} parameters: {1} thread: {2} target: {3}",
                                invocation.Method.Name,
                                string.Join(",", parameters),
                                Thread.CurrentThread.ManagedThreadId,
                                invocation.Proxy.GetHashCode());
            var start = DateTime.Now;
            try
            {
                invocation.Proceed();
            }
            catch (Exception e)
            {
                isFaulted = true;
                exception = e;
                //发生错误记录日志
                LogException(invocation, logger, e);
                throw;
            }
            finally
            {
                var taskResult = invocation.ReturnValue as Task;
                if (taskResult != null)
                {
                    taskResult.ContinueWith(t =>
                    {
                        object result = null;
                        if (t.IsFaulted)
                        {
                            isFaulted = true;
                            LogException(invocation, logger, t.Exception);
                        }
                        else
                        {
                            var returnType = invocation.Method.ReturnType;
                            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                            {
                                result = ((dynamic)t).Result;
                            }
                        }
                        LeaveMethod(invocation, logger, start, result, isFaulted, t.Exception);
                    });
                }
                else
                {
                    LeaveMethod(invocation, logger, start, invocation.ReturnValue, isFaulted, exception);
                }
            }
        }

        private static void LeaveMethod(IInvocation invocation, ILogger logger, DateTime start, object result, bool isFaulted, Exception e)
        {
            var costTime = (DateTime.Now - start).TotalMilliseconds;
            logger?.DebugFormat("Leave method: {0} isFaulted: {1} thread: {2} returnValue: {3} cost: {4} target: {5}",
                                invocation.Method.Name,
                                isFaulted,
                                e != null ? $"exception: {e.GetBaseException().Message} stackTrace: {e.GetBaseException().StackTrace}" : string.Empty,
                                Thread.CurrentThread.ManagedThreadId,
                                result,
                                costTime,
                                invocation.Proxy.GetHashCode());
        }

        private static void LogException(IInvocation invocation, ILogger logger, Exception e)
        {
            //发生错误记录日志
            logger?.ErrorFormat("Method: {0} threw exception: {1} {2} thread:{3} target: {4}",
                                invocation.Method.Name,
                                e.GetBaseException().Message,
                                e.GetBaseException().StackTrace,
                                Thread.CurrentThread.ManagedThreadId,
                                invocation.Proxy.GetHashCode());
        }
    }
}