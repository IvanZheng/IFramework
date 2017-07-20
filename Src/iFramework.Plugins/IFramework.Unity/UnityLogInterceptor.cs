using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity.InterceptionExtension;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace IFramework.Unity
{
    public class UnityLogInterceptor: LogInterceptionBehavior, IInterceptionBehavior
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
            var logger = GetTargetLogger(_loggerFactory, input.Target.GetType());

            BeforeInvoke(logger,
                         (MethodInfo)input.MethodBase,
                         input.Target,
                         input.Arguments
                              .Cast<object>()
                              .ToArray());

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
                        HandleException(logger,
                                        (MethodInfo)input.MethodBase, 
                                        input.Target,
                                        t.Exception);
                    }
                    else
                    {
                        var returnType = ((input.MethodBase as MethodInfo)?.ReturnType ?? t.GetType());
                        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            r = ((dynamic)t).Result;
                        }
                    }
                    AfterInvoke(logger,
                                (MethodInfo)input.MethodBase, 
                                input.Target,
                                start,
                                r, 
                                t.Exception);
                });
            }
            else
            {
                if (result.Exception != null)
                {
                    HandleException(logger, 
                                    (MethodInfo)input.MethodBase, 
                                    input.Target,
                                    result.Exception);
                }
                AfterInvoke(logger,
                            (MethodInfo)input.MethodBase,
                            input.Target,
                            start, 
                            result.ReturnValue, 
                            result.Exception);
            }
            return result;
        }

    }
}