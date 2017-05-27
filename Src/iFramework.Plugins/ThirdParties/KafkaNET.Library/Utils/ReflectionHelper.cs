using System;
using System.Reflection;

namespace Kafka.Client.Utils
{
    public static class ReflectionHelper
    {
        public static T Instantiate<T>(string className)
            where T : class
        {
            object o1;
            if (string.IsNullOrEmpty(className))
                return default(T);

            var t1 = Type.GetType(className, true);
            if (t1.IsGenericType)
            {
                var t2 = typeof(T).GetGenericArguments();
                var t3 = t1.MakeGenericType(t2);
                o1 = Activator.CreateInstance(t3);
                return o1 as T;
            }

            o1 = Activator.CreateInstance(t1);
            var obj = o1 as T;
            if (obj == null)
                throw new ArgumentException("Unable to instantiate class " + className + ", matching " +
                                            typeof(T).FullName);

            return obj;
        }

        public static T GetInstanceField<T>(string name, object obj)
            where T : class
        {
            var type = obj.GetType();
            var info = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            var value = info.GetValue(obj);
            return (T) value;
        }
    }
}