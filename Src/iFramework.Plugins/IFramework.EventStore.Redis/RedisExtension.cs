using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using StackExchange.Redis;

namespace IFramework.EventStore.Redis
{
    public static class RedisExtension
    {
        public static TResult GetValue<TResult>(this HashEntry[] hashEntries, string field, TResult defaultValue)
        {
            dynamic value = defaultValue;
            var hashEntry = hashEntries.FirstOrDefault(e => e.Name == field);
            if (hashEntry.Value.HasValue)
            {
                if (typeof(TResult) == typeof(string))
                {
                    value = hashEntry.Value.ToString();
                }
                else if (typeof(TResult) == typeof(long) && hashEntry.Value.TryParse(out long longValue))
                {
                    value = longValue;
                }
                else if (typeof(TResult) == typeof(int) && hashEntry.Value.TryParse(out int intValue))
                {
                    value = intValue;
                }
                else if ((typeof(TResult) == typeof(double) || typeof(TResult) == typeof(float)) && hashEntry.Value.TryParse(out double doubleValue))
                {
                    value = doubleValue;
                }
                else
                {
                    throw new InvalidCastException($"{field}: {hashEntry.Value}");
                }
            }

            return value;
        }

        public static HashEntry[] ToHashEntries(this object instance)
        {
            var propertiesInHashEntryList = new List<HashEntry>();
            foreach (var property in instance.GetType().GetProperties())
            {
                if(!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
                {
                    propertiesInHashEntryList.Add(new HashEntry(property.Name, property.GetValue(instance).ToJson()));
                }
                else
                {
                    propertiesInHashEntryList.Add(new HashEntry(property.Name, property.GetValue(instance).ToString()));
                }              
            }
            return propertiesInHashEntryList.ToArray();
        } 
    }
}
