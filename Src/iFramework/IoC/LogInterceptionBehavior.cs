using System;
using System.Collections.Generic;
using System.Reflection;
using IFramework.Infrastructure.Logging;

namespace IFramework.IoC
{
    public abstract class LogInterceptionBehavior : ILogInterceptionBehavior
    {
        public void BeforeInvoke(ILogger logger, MethodInfo method, object target, object[] arguments)
        {
            var serializeAttribute = GetLogInterceptionSerializeAttribute(method);

            var parameters = new Dictionary<string, object>();
            var methodParameters = method.GetParameters();
            for (var i = 0; i < Math.Min(arguments.Length, methodParameters.Length); i++)
            {
                var argument = serializeAttribute.SerializeArguments ? arguments[i] : arguments[i]?.ToString();
                parameters.Add(methodParameters[i].Name, argument);
            }
            logger?.Info(new AopEnteringLog
            {
                Method = method.Name,
                Target = target.GetHashCode().ToString(),
                Parameters = parameters
            });
        }

        public void AfterInvoke(ILogger logger, MethodInfo method, object target, DateTime start, object result, Exception exception)
        {
            var serializeAttribute = GetLogInterceptionSerializeAttribute(method);
            var costTime = (DateTime.Now - start).TotalMilliseconds;
            logger?.Info(new AopLeavingLog
            {
                Method = method.Name,
                Target = target.GetHashCode().ToString(),
                CostTime = costTime,
                Result = serializeAttribute.SerializeReturnValue ? result : result?.ToString()
            }, exception);
        }

        public void HandleException(ILogger logger, MethodInfo method, object target, Exception exception)
        {
            logger?.Error(new AopExceptionLog
            {
                Method = method.Name,
                Target = target.GetHashCode().ToString()
            }, exception);
        }

        private static LogInterceptionSerializeAttribute GetLogInterceptionSerializeAttribute(MethodInfo method)
        {
            return method.GetCustomAttribute<LogInterceptionSerializeAttribute>() ??
                   method.DeclaringType.GetCustomAttribute<LogInterceptionSerializeAttribute>() ??
                   new LogInterceptionSerializeAttribute();
        }

        protected virtual ILogger GetTargetLogger(ILoggerFactory loggerFactory, Type targetType)
        {
            var targetTypeName = targetType.Assembly.IsDynamic ? targetType.BaseType?.Name : targetType.Name;
            return loggerFactory.Create(!string.IsNullOrWhiteSpace(targetTypeName) ? targetTypeName : targetType.Name);
        }
    }
}