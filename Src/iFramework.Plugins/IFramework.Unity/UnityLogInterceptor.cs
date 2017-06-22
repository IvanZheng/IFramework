using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity.InterceptionExtension;
using IFramework.Infrastructure.Logging;

namespace IFramework.Unity
{
    public class UnityLogInterceptor: IInterceptionBehavior
    {
        private readonly ILoggerFactory _loggerFactory;

        public UnityLogInterceptor(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public bool WillExecute => true;

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            bool isFaulted = false;
            var inputType = input.Target.GetType();
            var inputTypeName = inputType.Assembly.IsDynamic ? inputType.BaseType?.Name : inputType.Name;

            var logger = _loggerFactory.Create(!string.IsNullOrWhiteSpace(inputTypeName) ? inputTypeName : inputType.Name);

            var parameters = new List<string>();
            for (var i = 0; i < input.Arguments.Count; i++)
            {
                parameters.Add($"{input.Arguments.ParameterName(i)}: {input.Arguments[i]}");
            }
            logger?.DebugFormat("Enter method: {0} parameters: {1} thread: {2} target: {3}",
                                input.MethodBase.Name,
                                string.Join(",", parameters),
                                Thread.CurrentThread.ManagedThreadId,
                                input.Target.GetHashCode());

            var start = DateTime.Now;
            var result = getNext()(input, getNext); //在这里执行方法

            var taskResult = result.ReturnValue as Task;
            if (taskResult != null)
            {
                taskResult.ContinueWith(t =>
                {
                    object r = null;
                    if (t.IsFaulted)
                    {
                        isFaulted = true;
                        LogException(input, logger, t.Exception);
                    }
                    else
                    {
                        var returnType = ((input.MethodBase as MethodInfo)?.ReturnType ?? t.GetType());
                        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            r = ((dynamic)t).Result;
                        }
                    }
                    LeaveMethod(input, logger, start, r, isFaulted, t.Exception);
                });
            }
            else
            {
                if (result.Exception != null)
                {
                    isFaulted = true;
                    LogException(input, logger, result.Exception);
                }
                LeaveMethod(input, logger, start, result.ReturnValue, isFaulted, result.Exception);
            }
            return result;
        }

        private static void LeaveMethod(IMethodInvocation input, ILogger logger, DateTime start, object result, bool isFaulted, Exception e)
        {
            var costTime = (DateTime.Now - start).TotalMilliseconds;
            logger?.DebugFormat("Leave method: {0} isFaulted: {1} thread: {2} returnValue: {3} cost: {4} target: {5}",
                                input.MethodBase.Name,
                                isFaulted,
                                e != null ? $"exception: {e.GetBaseException().Message} stackTrace: {e.GetBaseException().StackTrace}" : string.Empty,
                                Thread.CurrentThread.ManagedThreadId,
                                result,
                                costTime,
                                input.Target.GetHashCode());
        }

        private static void LogException(IMethodInvocation input, ILogger logger, Exception e)
        {
            //发生错误记录日志
            logger?.ErrorFormat("Method: {0} threw exception: {1} {2} thread: {3} target: {4}",
                                input.MethodBase.Name,
                                e.GetBaseException().Message,
                                e.GetBaseException().StackTrace,
                                Thread.CurrentThread.ManagedThreadId,
                                input.Target.GetHashCode());
        }
    }
}