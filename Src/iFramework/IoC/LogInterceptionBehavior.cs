using System;
using System.Collections.Generic;
using System.Reflection;
using IFramework.Infrastructure.Logging;

namespace IFramework.IoC
{
    public static class AopAction
    {
        public const string Enter = "enter";
        public const string Leave = "leave";
    }

    public abstract class LogInterceptionBehavior : ILogInterceptionBehavior
    {
        private readonly ILoggerFactory _loggerFactory;

        protected LogInterceptionBehavior(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

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
                Target = $"{target.GetType().FullName}({target.GetHashCode()})",
                Message = new
                {
                    Action = AopAction.Enter,
                    Parameters = parameters
                }
            });
        }

        public void AfterInvoke(ILogger logger, MethodInfo method, object target, DateTime start, object result, Exception exception)
        {
            var serializeAttribute = GetLogInterceptionSerializeAttribute(method);
            var costTime = (DateTime.Now - start).TotalMilliseconds;
            logger?.Info(new AopLeavingLog
            {
                Method = method.Name,
                Target = $"{target.GetType().FullName}({target.GetHashCode()})",
                Message = new
                {
                    Action = AopAction.Leave,
                    CostTime = costTime,
                    Result = serializeAttribute.SerializeReturnValue ? result : result?.ToString()
                }
            }, exception);
        }

        public void HandleException(ILogger logger, MethodInfo method, object target, Exception exception)
        {
            logger?.Error(new AopExceptionLog
            {
                Method = method.Name,
                Target = $"{target.GetType().FullName}({target.GetHashCode()})"
            }, exception);
        }

        private static LogInterceptionAttribute GetLogInterceptionSerializeAttribute(MethodInfo method)
        {
            return method.GetCustomAttribute<LogInterceptionAttribute>() ??
                   method.DeclaringType.GetCustomAttribute<LogInterceptionAttribute>() ??
                   new LogInterceptionAttribute();
        }

        protected virtual ILogger GetTargetLogger(Type targetType, MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var targetTypeName = method.GetCustomAttribute<LogInterceptionAttribute>()?.LoggerName ??
                                 method.DeclaringType.GetCustomAttribute<LogInterceptionAttribute>()?.LoggerName;

            if (string.IsNullOrWhiteSpace(targetTypeName))
            {
                targetTypeName = targetType.Assembly.IsDynamic ? targetType.BaseType?.FullName : targetType.FullName;
                targetTypeName = targetTypeName ?? targetType.FullName;
            }
            return _loggerFactory.Create(targetTypeName);
        }
    }
}