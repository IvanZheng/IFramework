using IFramework.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFramework.Tests
{
    [TestClass]
    public class UtilityTests
    {
        [TestMethod]
        public void InvokeTest()
        {
            var a = new A();
            var result = a.InvokeMethod(nameof(A.Method1), new object[] {1, null}) as string;
            Assert.IsTrue(result == "method1");

            result = a.InvokeMethod(nameof(A.Method1), new object[] {1, "2"}) as string;
            Assert.IsTrue(result == "method1");

            result = a.InvokeMethod(nameof(A.Method1), new object[] { 1, null }) as string;
            Assert.IsTrue(result == "method1");

            result = a.InvokeMethod(nameof(A.Method1), new object[] {1, 2}) as string;
            Assert.IsTrue(result == "method2");

            result = a.InvokeGenericMethod(nameof(A.Method1), new object[] {1, "2", 1, null}, typeof(int), typeof(string)) as string;
            Assert.IsTrue(result == "method3");

            result = a.InvokeGenericMethod(nameof(A.Method1), new object[] { 1, "2", "2", 1 }, typeof(int), typeof(string)) as string;
            Assert.IsTrue(result == "method3");

            result = a.InvokeGenericMethod(nameof(A.Method1), new object[] {1, 2, null, "1"}, typeof(string), typeof(string)) as string;
            Assert.IsTrue(result == "method4");
        }
    }

    public class A
    {
        public string Method1(int i, string j)
        {
            return $"method1";
        }

        public string Method1(int i, int j)
        {
            return $"method2";
        }

        public string Method1<T, S>(int i, string j, T k, S l)
        {
            return $"method3";
        }
        public string Method1<T, S>(int i, string j, S k, T l)
        {
            return $"method3";
        }
        public string Method1<T, S>(int i, int j, T k, S l)
        {
            return $"method4";
        }
    }
}