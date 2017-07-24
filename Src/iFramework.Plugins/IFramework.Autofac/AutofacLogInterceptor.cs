using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;

namespace IFramework.Autofac
{
    public class AutofacLogInterceptor: LogInterceptionBehavior, IInterceptor
    {
        public AutofacLogInterceptor(ILoggerFactory loggerFactory):base(loggerFactory)
        {
        }


        public void Intercept(IInvocation invocation)
        {
            Exception exception = null;

            var logger = GetTargetLogger(invocation.TargetType, invocation.Method);
            
            BeforeInvoke(logger, invocation.Method, invocation.Proxy, invocation.Arguments);

            var start = DateTime.Now;
            try
            {
                invocation.Proceed();
            }
            catch (Exception e)
            {
                exception = e;
                //发生错误记录日志
                HandleException(logger, invocation.Method, invocation.Proxy, e);
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
                            HandleException(logger, invocation.Method, invocation.Proxy, t.Exception);
                        }
                        else
                        {
                            var returnType = invocation.Method.ReturnType;
                            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                            {
                                result = ((dynamic)t).Result;
                            }
                        }
                        AfterInvoke(logger, invocation.Method, invocation.Proxy, start, result, t.Exception);
                    });
                }
                else
                {
                    AfterInvoke(logger, invocation.Method, invocation.Proxy, start, invocation.ReturnValue, exception);
                }
            }
        }
    }
}