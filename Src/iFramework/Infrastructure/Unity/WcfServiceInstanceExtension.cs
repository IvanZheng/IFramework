using System.ServiceModel;

namespace IFramework.Infrastructure.Unity
{
    /// <summary>
    /// Represents the WCF Service Instance extension of the <see cref="InstanceContext"/> class.
    /// </summary>
    internal class WcfServiceInstanceExtension : IExtension<InstanceContext>
    {
        #region Private Fields
        private readonly InstanceItems items = new InstanceItems();
        #endregion

        #region Internal Properties
        /// <summary>
        /// Gets the instance of <see cref="InstanceItems"/> object.
        /// </summary>
        internal InstanceItems Items { get { return items; } }
        #endregion

        #region Internal Static Properties
        /// <summary>
        /// Gets the current instance of <see cref="WcfServiceInstanceExtension"/> class.
        /// </summary>
        internal static WcfServiceInstanceExtension Current
        {
            get
            {
                if (OperationContext.Current == null)
                    return null;
                var instanceContext = OperationContext.Current.InstanceContext;
                return GetExtensionFrom(instanceContext);
            }
        }
        #endregion

        #region Private Methods
        private static WcfServiceInstanceExtension GetExtensionFrom(InstanceContext instanceContext)
        {
            lock (instanceContext)
            {
                WcfServiceInstanceExtension extension = instanceContext.Extensions.Find<WcfServiceInstanceExtension>();
                if (extension == null)
                {
                    extension = new WcfServiceInstanceExtension();
                    extension.Items.Hook(instanceContext);
                    instanceContext.Extensions.Add(extension);
                }
                return extension;
            }
        }
        #endregion

        #region IExtension<InstanceContext> Members
        /// <summary>
        /// Enables an extension object to find out when it has been aggregated. Called
        /// when the extension is added to the <see cref="System.ServiceModel.IExtensibleObject&lt;T&gt;.Extensions"/>
        /// property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Attach(InstanceContext owner) { }
        /// <summary>
        /// Enables an object to find out when it is no longer aggregated. Called when
        /// an extension is removed from the <see cref="System.ServiceModel.IExtensibleObject&lt;T&gt;.Extensions"/>
        /// property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Detach(InstanceContext owner) { }

        #endregion
    }
}
