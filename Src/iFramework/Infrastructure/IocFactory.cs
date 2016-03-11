using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;

namespace IFramework.Infrastructure
{
    public sealed class IoCFactory
    {
        #region Singleton

        static readonly IoCFactory instance = new IoCFactory();

        /// <summary>
        /// Get singleton instance of IoCFactory
        /// </summary>
        public static IoCFactory Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region Members

        static IUnityContainer _CurrentContainer;

        /// <summary>
        /// Get current configured IContainer
        /// <remarks>
        /// At this moment only IoCUnityContainer existss
        /// </remarks>
        /// </summary>
        public IUnityContainer CurrentContainer
        {
            get
            {
                return _CurrentContainer;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Only for singleton pattern, remove before field init IL anotation
        /// </summary>
        static IoCFactory() 
        {
            _CurrentContainer = new UnityContainer();
            try
            {
                _CurrentContainer.LoadConfiguration();
            }
            catch (Exception)
            {
            }
            //UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection(UnityConfigurationSection.SectionName);
            //if (section != null)
            //{
            //    section.Configure(_CurrentContainer);
            //}
        }

        public static T Resolve<T>(string name, params ResolverOverride[] overrides)
        {
            return IoCFactory.Instance.CurrentContainer.Resolve<T>(name, overrides);
        }

        public static T Resolve<T>(params ResolverOverride[] overrides)
        {
            return IoCFactory.Instance.CurrentContainer.Resolve<T>(overrides);
        }

        public static object Resolve(Type type, params ResolverOverride[] overrides)
        {
            return IoCFactory.Instance.CurrentContainer.Resolve(type, overrides);
        }

        public static object Resolve(Type type, string name, params ResolverOverride[] overrides)
        {
            return IoCFactory.Instance.CurrentContainer.Resolve(type, name, overrides);
        }

        #endregion
    }
}
