using IFramework.Command;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace IFramework.Command.Impl
{
    class NullPropertyInfo : _MemberInfo
    {
        public Type DeclaringType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public MemberTypes MemberType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Type ReflectedType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public void GetIDsOfNames(ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        public void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        public void GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        public void Invoke(uint dispIdMember, ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
    public class LinearCommandManager : ILinearCommandManager
    {
        Hashtable LinearFuncs = new Hashtable();
        ConcurrentDictionary<Type, _MemberInfo> CommandLinerKeys = new ConcurrentDictionary<Type, _MemberInfo>();
        public LinearCommandManager()
        {
        }

        public object GetLinearKey(ILinearCommand command)
        {
            return this.InvokeGenericMethod(command.GetType(), "GetLinearKeyImpl", new object[] { command });
        }

        public object GetLinearKeyImpl<TLinearCommand>(TLinearCommand command) where TLinearCommand : ILinearCommand
        {
            object linearKey = null;
            Func<TLinearCommand, object> func = LinearFuncs[typeof(TLinearCommand)] as Func<TLinearCommand, object>;
            if (func != null)
            {
                linearKey = func(command);
            }
            else
            {
                var propertyWithKeyAttribute = CommandLinerKeys.GetOrAdd(command.GetType(), (type) => {
                    var keyProperty = command.GetType().GetProperties()
                                                  .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(LinearKeyAttribute)))
                                                  .FirstOrDefault() as _MemberInfo;
                    if (keyProperty == null)
                    {
                        keyProperty = new NullPropertyInfo();
                    }
                    return keyProperty;
                });

                if (propertyWithKeyAttribute is NullPropertyInfo)
                {
                    linearKey = typeof(TLinearCommand).Name;
                }
                else
                {
                   linearKey = command.GetValueByKey(propertyWithKeyAttribute.Name);
                }
            }
            return linearKey;
        }

        public void RegisterLinearCommand<TLinearCommand>(Func<TLinearCommand, object> func) where TLinearCommand : ILinearCommand
        {
            LinearFuncs.Add(typeof(TLinearCommand), func);
        }
    }
}
