using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Infrastructure.Unity.LifetimeManagers
{
    public class PerMessageOrExecutionContextLifetimeManager :
        LifetimeManager
    {
        Guid _key;

        PerMessageContextLifetimeManager _perMessageContextLifetimeManager;
        PerExecutionContextLifetimeManager _perExecutionContextLifetimeManager;
        public PerMessageOrExecutionContextLifetimeManager() : this(Guid.NewGuid()) { }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="key">A key for this lifetimemanager resolver</param>
        PerMessageOrExecutionContextLifetimeManager(Guid key)
        {
            if (key == Guid.Empty)
                throw new ArgumentException("PerMessageOrExecutionContextLifetimeManagerKeyCannotBeNull");
            _key = key;
            _perMessageContextLifetimeManager = new PerMessageContextLifetimeManager(key);
            _perExecutionContextLifetimeManager = new PerExecutionContextLifetimeManager(key);
        }

        public override object GetValue()
        {
            object value = null;
            if (PerMessageContextLifetimeManager.CurrentMessageContext != null)
            {
                value = _perMessageContextLifetimeManager.GetValue();
            }
            else
            {
                value = _perExecutionContextLifetimeManager.GetValue();
            }
            return value;
        }

        public override void RemoveValue()
        {
            if (PerMessageContextLifetimeManager.CurrentMessageContext != null)
            {
                _perMessageContextLifetimeManager.RemoveValue();
            }
            else
            {
                _perExecutionContextLifetimeManager.RemoveValue();
            }
        }

        public override void SetValue(object newValue)
        {
            if (PerMessageContextLifetimeManager.CurrentMessageContext != null)
            {
                _perMessageContextLifetimeManager.SetValue(newValue);
            }
            else
            {
                _perExecutionContextLifetimeManager.SetValue(newValue);
            }
        }
    }
}
