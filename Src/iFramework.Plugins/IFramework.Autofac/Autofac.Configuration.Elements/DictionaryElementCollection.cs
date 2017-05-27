using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using Autofac.Configuration.Util;

namespace Autofac.Configuration.Elements
{
    [TypeConverter(typeof(DictionaryElementTypeConverter))]
    public class DictionaryElementCollection : ConfigurationElementCollection<ListItemElement>
    {
        public DictionaryElementCollection() : base("item")
        {
        }

        private class DictionaryElementTypeConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                Type destinationType)
            {
                var instantiableType = GetInstantiableType(destinationType);
                var dictionaryElementCollection = value as DictionaryElementCollection;
                if (dictionaryElementCollection != null && instantiableType != null)
                {
                    var dictionary = (IDictionary) Activator.CreateInstance(instantiableType);
                    var genericArguments = instantiableType.GetGenericArguments();
                    foreach (var current in dictionaryElementCollection)
                    {
                        if (string.IsNullOrEmpty(current.Key))
                            throw new ConfigurationErrorsException("Key cannot be null in a dictionary element.");
                        var key = TypeManipulation.ChangeToCompatibleType(current.Key, genericArguments[0], null);
                        var value2 = TypeManipulation.ChangeToCompatibleType(current.Value, genericArguments[1], null);
                        dictionary.Add(key, value2);
                    }
                    return dictionary;
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return GetInstantiableType(destinationType) != null || base.CanConvertTo(context, destinationType);
            }

            private static Type GetInstantiableType(Type destinationType)
            {
                if (typeof(IDictionary).IsAssignableFrom(destinationType) || destinationType.IsGenericType &&
                    typeof(IDictionary<,>).IsAssignableFrom(destinationType.GetGenericTypeDefinition()))
                {
                    var array = destinationType.IsGenericType
                        ? destinationType.GetGenericArguments()
                        : new[]
                        {
                            typeof(string),
                            typeof(object)
                        };
                    if (array.Length != 2)
                        return null;
                    var type = typeof(Dictionary<,>).MakeGenericType(array);
                    if (destinationType.IsAssignableFrom(type))
                        return type;
                }
                return null;
            }
        }
    }
}