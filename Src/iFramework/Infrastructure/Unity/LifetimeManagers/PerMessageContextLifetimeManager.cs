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
    public class EmptyMessageContext : IMessageContext
    {
        public EmptyMessageContext()
        {

        }
        public EmptyMessageContext(IFramework.Message.IMessage message)
        {
            SentTime = DateTime.Now;
            Message = message;
            MessageID = message.ID;
        }
        public IDictionary<string, object> Headers
        {
            get { return null; }
        }

        public string Key
        {
            get { return null; }
        }

        public string MessageID
        {
            get;
            set;
        }

        public string ReplyToEndPoint
        {
            get { return null; }
        }

        public object Reply
        {
            get;
            set;
        }

        public string FromEndPoint
        {
            get;
            set;
        }

        public object Message
        {
            get;
            set;
        }

        public DateTime SentTime
        {
            get;
            set;
        }


        public string CorrelationID
        {
            get;
            set;
        }


        public string Topic
        {
            get;
            set;
        }


        public List<IMessageContext> ToBeSentMessageContexts
        {
            get { return null; }
        }
    }

    class MessageContextWrapper
    {
        internal Hashtable Items { get; set; }
        internal IMessageContext MessageContext { get; set; }
        internal MessageContextWrapper(IMessageContext messageContext)
        {
            Items = new Hashtable();
            MessageContext = messageContext;
        }
    }

    public sealed class PerMessageContextLifetimeManager
        : LifetimeManager
    {
        //static EmptyMessageContext EmptyMessageContext;
        static PerMessageContextLifetimeManager PerMessageContextLifeTimeManager;
        Guid _key;

        static PerMessageContextLifetimeManager()
        {
            //EmptyMessageContext = new EmptyMessageContext();
            PerMessageContextLifeTimeManager = new PerMessageContextLifetimeManager();
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessageContext>(PerMessageContextLifeTimeManager);
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
        internal PerMessageContextLifetimeManager(Guid key)
        {
            if (key == Guid.Empty)
                throw new ArgumentException("PerExecutionContextLifetimeManagerKeyCannotBeNull");

            _key = key;
        }
        #endregion

        static Hashtable CurrentMessageContextItems
        {
            get
            {
                Hashtable items = null;
                var messageContextWrapper = CallContext.GetData("MessageContext") as MessageContextWrapper;
                if (messageContextWrapper != null)
                {
                    items = messageContextWrapper.Items;
                }
                return items;
            }
        }

        public static IMessageContext CurrentMessageContext
        {
            get
            {
                IMessageContext messageContext = null;
                var messageContextWrapper = CallContext.GetData("MessageContext") as MessageContextWrapper;
                if (messageContextWrapper != null)
                {
                    messageContext = messageContextWrapper.MessageContext;
                }
                return messageContext;
            }
            set
            {
                //if (value == null)
                //{
                ClearCurrentMessageContextItems();
                CallContext.FreeNamedDataSlot("MessageContext");
                // }
                if (value != null)
                {
                    CallContext.SetData("MessageContext", new MessageContextWrapper(value));
                }
                PerMessageContextLifeTimeManager.SetValue(value);
            }
        }



        #region ILifetimeManager Members

        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.GetValue"/>
        /// </summary>
        /// <returns><see cref="M:Microsoft.Practices.Unity.LifetimeManager.GetValue"/></returns>
        public override object GetValue()
        {
            object result = null;
            var items = CurrentMessageContextItems;
            if (items != null)
            {
                result = items[_key];
            }
            return result;
        }
        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.RemoveValue"/>
        /// </summary>
        public override void RemoveValue()
        {
            object value = null;
            if (CurrentMessageContextItems != null)
            {
                value = CurrentMessageContextItems[_key];
                if (value != null)
                {
                    CurrentMessageContextItems.Remove(_key);
                    if (value is IDisposable)
                    {
                        (value as IDisposable).Dispose();
                    }
                }
            }
        }
        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.SetValue"/>
        /// </summary>
        /// <param name="newValue"><see cref="M:Microsoft.Practices.Unity.LifetimeManager.SetValue"/></param>
        public override void SetValue(object newValue)
        {
            if (CurrentMessageContextItems != null && newValue != null)
            {
                CurrentMessageContextItems.Add(_key, newValue);
            }
        }

        #endregion

        static void ClearCurrentMessageContextItems()
        {
            var items = CurrentMessageContextItems;
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
            }
        }
    }
}
