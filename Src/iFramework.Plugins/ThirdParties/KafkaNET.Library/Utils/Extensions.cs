using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Client.Utils
{
    public static class Extensions
    {
        public static string ToMultiString<T>(this IEnumerable<T> items, string separator)
        {
            if (!items.Any())
                return "NULL";

            return string.Join(separator, items);
        }

        public static string ToMultiString<T>(this IEnumerable<T> items, Expression<Func<T, object>> selector,
            string separator)
        {
            if (!items.Any())
                return "NULL";

            var compiled = selector.Compile();
            return string.Join(separator, items.Select(compiled));
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            if (action == null)
                throw new ArgumentNullException("action");

            foreach (var item in items)
                action(item);
        }
    }
}