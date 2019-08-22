using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Xunit;

namespace IFramework.Test
{
    public class UtilityTests
    {
        [Fact]
        public async Task ThreadContextTest()
        {

        }


        [Fact]
        public void InvokeTest()
        {
            var a = new AA();
            var result = a.InvokeMethod(nameof(AA.Method1), new object[] { 1, null }) as string;
            Assert.True(result == "method1");

            result = a.InvokeMethod(nameof(AA.Method1), new object[] { 1, "2" }) as string;
            Assert.True(result == "method1");

            result = a.InvokeMethod(nameof(AA.Method1), new object[] { 1, null }) as string;
            Assert.True(result == "method1");

            result = a.InvokeMethod(nameof(AA.Method1), new object[] { 1, 2 }) as string;
            Assert.True(result == "method2");

            result = a.InvokeGenericMethod(nameof(AA.Method1), new object[] { 1, "2", 1, null }, typeof(int), typeof(string)) as string;
            Assert.True(result == "method3");

            result = a.InvokeGenericMethod(nameof(AA.Method1), new object[] { 1, "2", "2", 1 }, typeof(int), typeof(string)) as string;
            Assert.True(result == "method3");

            result = a.InvokeGenericMethod(nameof(AA.Method1), new object[] { 1, 2, null, "1" }, typeof(string), typeof(string)) as string;
            Assert.True(result == "method4");


            result = typeof(AA).InvokeStaticGenericMethod(nameof(AA.Method2), new object[] { 1, "2", 1, null }, typeof(int), typeof(string)) as string;
            Assert.True(result == "static method3");

        }
    }

    public class AA
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

        public static string Method2<T, S>(int i, string j, T k, S l)
        {
            return $"static method3";
        }

        public static string Method2<T, S>(int i, int j, T k, S l)
        {
            return $"static method4";
        }
    }
}
