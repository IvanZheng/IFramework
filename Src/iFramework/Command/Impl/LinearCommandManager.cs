using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using IFramework.Infrastructure;

namespace IFramework.Command.Impl
{
  

    public class LinearCommandManager : ILinearCommandManager
    {
        private readonly ConcurrentDictionary<Type, MemberInfo> _commandLinerKeys =
            new ConcurrentDictionary<Type, MemberInfo>();

        private readonly Hashtable _linearFuncs = new Hashtable();

        public object GetLinearKey(ILinearCommand command)
        {
            return this.InvokeGenericMethod("GetLinearKeyImpl", new object[] {command}, command.GetType());
        }

        public void RegisterLinearCommand<TLinearCommand>(Func<TLinearCommand, object> func)
            where TLinearCommand : ILinearCommand
        {
            _linearFuncs.Add(typeof(TLinearCommand), func);
        }

        public object GetLinearKeyImpl<TLinearCommand>(TLinearCommand command) where TLinearCommand : ILinearCommand
        {
            object linearKey = null;
            if (_linearFuncs[typeof(TLinearCommand)] is Func<TLinearCommand, object> func)
            {
                linearKey = func(command);
            }
            else
            {
                var propertyWithKeyAttribute = _commandLinerKeys.GetOrAdd(command.GetType(), type =>
                {
                    var keyProperty = command.GetType()
                                             .GetProperties()
                                             .FirstOrDefault(p => p.GetCustomAttribute<LinearKeyAttribute>() != null) as MemberInfo;
                    return keyProperty;
                });

                linearKey = propertyWithKeyAttribute == null ? typeof(TLinearCommand).Name : command.GetPropertyValue(propertyWithKeyAttribute.Name);
            }
            return linearKey;
        }
    }
}