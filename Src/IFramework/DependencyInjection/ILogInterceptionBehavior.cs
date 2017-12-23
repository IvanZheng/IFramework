using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;

namespace IFramework.DependencyInjection
{
    public interface ILogInterceptionBehavior
    {
        void BeforeInvoke(ILogger logger, MethodInfo method, object target, object[] arguments);
        void AfterInvoke(ILogger logger, MethodInfo method, object target, DateTime start, object result, Exception exception);
        void HandleException(ILogger logger, MethodInfo method, object target, Exception exception);
    }
}
