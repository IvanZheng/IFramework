using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using IFramework.Infrastructure;

namespace IFramework.Command.Impl
{
  

    public class SerialCommandManager : ISerialCommandManager
    {
        private readonly ConcurrentDictionary<Type, MemberInfo> _commandSerialKeys =
            new ConcurrentDictionary<Type, MemberInfo>();

        private readonly Hashtable _serialFunctions = new Hashtable();

        public object GetLinearKey(ILinearCommand command)
        {
            return this.InvokeGenericMethod("GetLinearKeyImpl", new object[] {command}, command.GetType());
        }

        public void RegisterSerialCommand<TLinearCommand>(Func<TLinearCommand, object> func)
            where TLinearCommand : ILinearCommand
        {
            _serialFunctions.Add(typeof(TLinearCommand), func);
        }

        public object GetLinearKeyImpl<TLinearCommand>(TLinearCommand command) where TLinearCommand : ILinearCommand
        {
            object linearKey = null;
            if (_serialFunctions[typeof(TLinearCommand)] is Func<TLinearCommand, object> func)
            {
                linearKey = func(command);
            }
            else
            {
                var propertyWithKeyAttribute = _commandSerialKeys.GetOrAdd(command.GetType(), type =>
                {
                    var keyProperty = command.GetType()
                                             .GetProperties()
                                             .FirstOrDefault(p => p.GetCustomAttribute<SerialKeyAttribute>() != null) as MemberInfo;
                    return keyProperty;
                });

                linearKey = propertyWithKeyAttribute == null ? typeof(TLinearCommand).Name : command.GetPropertyValue(propertyWithKeyAttribute.Name);
            }
            return linearKey;
        }
    }
}