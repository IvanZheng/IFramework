using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEOP.AutofacTests;
using IFramework.Config;
using IFramework.IoC;
using System;
using System.Reflection;

namespace IFramework.Autofac.Tests
{
    [TestClass()]
    public class ObjectContainerTests
    {
        [TestMethod()]
        public void ResolveAutofac()
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                         ;
            for (int i = 0; i < 10000; i++)
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                var myClass = scope.Resolve<MyClass>();
                Assert.AreEqual(myClass.Container, myClass.YourClass.Container);
            }
        }

        [TestMethod()]
        public void ResolveUnity()
        {
            Configuration.Instance
                         .UseUnityContainer()
                         ;
            for (int i = 0; i < 10000; i++)
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            { 
                var myClass = scope.Resolve<MyClass>();
                Assert.AreEqual(myClass.Container, myClass.YourClass.Container);
            }

        }
    }
}