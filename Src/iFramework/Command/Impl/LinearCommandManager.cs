using IFramework.Command;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.Command.Impl
{
    public class LinearCommandManager : ILinearCommandManager
    {
        Hashtable LinearFuncs = new Hashtable();

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
                linearKey = typeof(TLinearCommand).Name;
            }
            return linearKey;
        }

        public void RegisterLinearCommand<TLinearCommand>(Func<TLinearCommand, object> func) where TLinearCommand : ILinearCommand
        {
            LinearFuncs.Add(typeof(TLinearCommand), func);
        }
    }
}
