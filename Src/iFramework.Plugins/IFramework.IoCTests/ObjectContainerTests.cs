using System.Reflection;
using IFramework.AutofacTests;
using IFramework.Config;
using IFramework.IoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFramework.Autofac.Tests
{
    [TestClass]
    public class ObjectContainerTests
    {
        [TestMethod]
        public void ResolveAutofac()
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                ;
            for (var i = 0; i < 10000; i++)
            {
                using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                {
                    var myClass = scope.Resolve<MyClass>();
                    Assert.AreEqual(myClass.Container, myClass.YourClass.Container);
                }
            }
        }

        [TestMethod]
        public void ResolveUnity()
        {
            Configuration.Instance
                         .UseUnityContainer()
                ;
            for (var i = 0; i < 10000; i++)
            {
                using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                {
                    var myClass = scope.Resolve<MyClass>();
                    Assert.AreEqual(myClass.Container, myClass.YourClass.Container);
                }
            }
        }
    }
}