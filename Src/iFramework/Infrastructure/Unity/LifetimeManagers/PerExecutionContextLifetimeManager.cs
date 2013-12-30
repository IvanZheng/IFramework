using System;
using System.Web;
using System.ServiceModel;
using System.Runtime.Remoting.Messaging;
using Microsoft.Practices.Unity;
using System.Linq;

namespace IFramework.Infrastructure.Unity.LifetimeManagers
{
    internal class ContainerExtension : IExtension<OperationContext>
    {
        #region Members
        public Guid Key { get; set; }
        public object Value { get; set; }

        #endregion

        #region IExtension<OperationContext> Members

        public void Attach(OperationContext owner)
        {

        }

        public void Detach(OperationContext owner)
        {

        }

        #endregion
    }
    /// <summary>
    /// This is a custom lifetime that reuses an instance of a class, on the same
    /// execution environment. For example, within a WCF request or ASP.NET request, all objects created using this
    /// LifeTimeManager will be shared.
    /// </summary>
    sealed class PerExecutionContextLifetimeManager
        : LifetimeManager
    {
        #region Nested

        /// <summary>
        /// Custom extension for OperationContext scope
        /// </summary>
     
        #endregion

        #region Members

        Guid _key;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public PerExecutionContextLifetimeManager() : this(Guid.NewGuid()) { }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="key">A key for this lifetimemanager resolver</param>
        PerExecutionContextLifetimeManager(Guid key)
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

            //Get object depending on  execution environment ( WCF without HttpContext,HttpContext or CallContext)

            if (OperationContext.Current != null)
            {
                //WCF without HttpContext environment
                result = WcfServiceInstanceExtension.Current.Items.Find(_key);
             
            }
            else if (HttpContext.Current != null)
            {
                //HttpContext avaiable ( ASP.NET ..)
                if (HttpContext.Current.Items[_key.ToString()] != null)
                    result = HttpContext.Current.Items[_key.ToString()];
            }
            else
            {
                //Not in WCF or ASP.NET Environment, UnitTesting, WinForms, WPF etc.
                result = CallContext.GetData(_key.ToString());
            }

            return result;
        }
        /// <summary>
        /// <see cref="M:Microsoft.Practices.Unity.LifetimeManager.RemoveValue"/>
        /// </summary>
        public override void RemoveValue()
        {
            object value = null;
            if (OperationContext.Current != null)
            {
                //WCF without HttpContext environment
                WcfServiceInstanceExtension.Current.Items.Remove(_key);
            }
            else if (HttpContext.Current != null)
            {
                //HttpContext avaiable ( ASP.NET ..)
                if (HttpContext.Current.Items[_key.ToString()] != null)
                {
                    value = HttpContext.Current.Items[_key.ToString()];
                    HttpContext.Current.Items[_key.ToString()] = null;
                }
            }
            else
            {
                //Not in WCF or ASP.NET Environment, UnitTesting, WinForms, WPF etc.
                value = CallContext.GetData(_key.ToString());
                CallContext.FreeNamedDataSlot(_key.ToString());
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

            if (OperationContext.Current != null)
            {
                //WCF without HttpContext environment
                WcfServiceInstanceExtension.Current.Items.Set(_key, newValue);
            }
            else if (HttpContext.Current != null)
            {
                //HttpContext avaiable ( ASP.NET ..)
                if (HttpContext.Current.Items[_key.ToString()] == null)
                    HttpContext.Current.Items[_key.ToString()] = newValue;
            }
            else
            {
                //Not in WCF or ASP.NET Environment, UnitTesting, WinForms, WPF etc.
                CallContext.SetData(_key.ToString(), newValue);
            }
        }

        #endregion
    }
}
