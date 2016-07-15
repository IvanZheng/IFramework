using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IFramework.IoC
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

        static IContainer _CurrentContainer;

        /// <summary>
        /// Get current configured IContainer
        /// <remarks>
        /// At this moment only IoCUnityContainer existss
        /// </remarks>
        /// </summary>
        public IContainer CurrentContainer
        {
            get
            {
                if (_CurrentContainer == null)
                {
                    throw new Exception("Please call SetContainer first.");
                }
                return _CurrentContainer;
            }
        }

        public static IContainer SetContainer(IContainer container)
        {
            _CurrentContainer = container;
            return _CurrentContainer;
        }

        #endregion

        #region Constructor

        public static T Resolve<T>(string name, params Parameter[] parameters)
        {
            return Instance.CurrentContainer.Resolve<T>(name, parameters);
        }

        public static T Resolve<T>(params Parameter[] parameters)
        {
            return Instance.CurrentContainer.Resolve<T>(parameters);
        }

        public static object Resolve(Type type, params Parameter[] parameters)
        {
            return Instance.CurrentContainer.Resolve(type, parameters);
        }

        public static object Resolve(Type type, string name, params Parameter[] parameters)
        {
            return Instance.CurrentContainer.Resolve(type, name, parameters);
        }

        #endregion
    }
}
