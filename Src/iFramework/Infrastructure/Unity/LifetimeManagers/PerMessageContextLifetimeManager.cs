using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using IFramework.Infrastructure;
using System.Collections;
using IFramework.Message;
using System.Runtime.Remoting.Messaging;

namespace IFramework.Infrastructure.Unity.LifetimeManagers
{
    public static class MessageContextExtension
    {
        public static void ClearItems(this IMessageContext context)
        {
            PerMessageContextLifetimeManager.Remove(context);
            CallContext.FreeNamedDataSlot("MessageContext");
        }
    }


    public sealed class PerMessageContextLifetimeManager
        : LifetimeManager
    {

        static Hashtable MessageContextItems = Hashtable.Synchronized(new Hashtable());
        Guid _key;

        public static IMessageContext CurrentMessageContext
        {
            get
            {
                return CallContext.GetData("MessageContext") as IMessageContext;
            }
            set
            {
                if (value == null)
                {
                    CallContext.FreeNamedDataSlot("MessageContext");
                }
                else
                {
                    CallContext.SetData("MessageContext", value);
                }
            }
        }

        Hashtable CurrentContextItems
        {
            get
            {
                var currentMessageContext = CurrentMessageContext;
                if (currentMessageContext == null)
                {
                    return null;
                }
                var items = MessageContextItems[currentMessageContext.MessageID] as Hashtable;
                if (items == null)
                {
                    items = new Hashtable();
                    MessageContextItems.Add(currentMessageContext.MessageID, items);
                }
                return items;
            }
        }

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public PerMessageContextLifetimeManager() : this(Guid.NewGuid()) { }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="key">A key for this lifetimemanager resolver</param>
        PerMessageContextLifetimeManager(Guid key)
        {
            if (key == Guid.Empty)
                throw new ArgumentException("PerExecutionContextLifetimeManagerKeyCannotBeNull");

            _key = key;
        }
        #endregion

        #region ILifetimeManager Members

        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.GetValue"/>
        /// </summary>
        /// <returns><see cref="M:Microsoft.Practices.Unity.LifetimeManager.GetValue"/></returns>
        public override object GetValue()
        {
            object result = null;
            if (CurrentMessageContext != null)
            {
                //HttpContext avaiable ( ASP.NET ..)
                if (CurrentContextItems[_key] != null)
                    result = CurrentContextItems[_key];
            }

            return result;
        }
        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.RemoveValue"/>
        /// </summary>
        public override void RemoveValue()
        {
            object value = null;
            if (CurrentMessageContext != null)
            {
                //WCF without HttpContext environment
                value = CurrentContextItems[_key];
                if (value != null)
                {
                    CurrentContextItems.Remove(_key);
                }
            }
            
            if (value is IDisposable)
            {
                (value as IDisposable).Dispose();
            }
        }
        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.SetValue"/>
        /// </summary>
        /// <param name="newValue"><see cref="M:Microsoft.Practices.Unity.LifetimeManager.SetValue"/></param>
        public override void SetValue(object newValue)
        {
            if (CurrentMessageContext != null)
            {
                //WCF without HttpContext environment
                CurrentContextItems.Add(_key, newValue);
            }
        }

        #endregion

        internal static void Remove(IMessageContext context)
        {
            var items = MessageContextItems[context.MessageID] as Hashtable;
            if (items != null)
            {
                foreach (var value in items.Values)
                {
                    if (value is IDisposable)
                    {
                        (value as IDisposable).Dispose();
                    }
                }
                items.Clear();
                MessageContextItems.TryRemove(context.MessageID);
            }
        }
    }
}
