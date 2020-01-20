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
            var hashEntry = hashEntries.FirstOrDefault(e => e.Name == field);
            if (hashEntry.Value.HasValue)
            {
                return (TResult)hashEntry.Value.Box();
            }

            return defaultValue;
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
