using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using Microsoft.Practices.Unity;
using System.Web;
using System.Reflection;

namespace IFramework.Infrastructure
{
    public class IoCPageHandlerFactory : PageHandlerFactory
    {

        private static object GetInstance(Type type)
        {
            // Change this line if you're not using the CSL,
            // but a DI framework directly.
            //return Microsoft.Practices.ServiceLocation
            //    .ServiceLocator.Current.GetInstance(type);
            return IoCFactory.Instance.CurrentContainer.Resolve(type);
        }

        public override IHttpHandler GetHandler(HttpContext context,
            string requestType, string virtualPath, string path)
        {
            var page =
                base.GetHandler(context, requestType, virtualPath, path);
            
            if (page != null)
            {
                InjectDependencies(page);
            }

            return page;
        }

        private static void InjectDependencies(object page)
        {
            Type pageType = page.GetType().BaseType;

            var ctor = GetInjectableConstructor(pageType);

            if (ctor != null)
            {
                try
                {
                    object[] arguments =
                        GetConstructorArguments(ctor);

                    ctor.Invoke(page, arguments);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format(
                        "The type {0} could not be initialized. {1}",
                        pageType, ex.Message), ex);
                }
            }
        }

        private static object[] GetConstructorArguments(
            ConstructorInfo ctor)
        {
            var parameters = ctor.GetParameters();

            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                arguments[i] =
                    GetInstance(parameters[i].ParameterType);
            }

            return arguments;
        }

        private static ConstructorInfo GetInjectableConstructor(
            Type type)
        {
            var overloadedPublicConstructors = (
                from ctor in type.GetConstructors()
                where ctor.GetParameters().Length > 0
                select ctor).ToArray();

            if (overloadedPublicConstructors.Length == 0)
            {
                return null;
            }

            if (overloadedPublicConstructors.Length == 1)
            {
                return overloadedPublicConstructors[0];
            }

            throw new Exception(string.Format(
                "The type {0} has multiple public overloaded " +
                "constructors and can't be initialized.", type));
        }
    }
}
