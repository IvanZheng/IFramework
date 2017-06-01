using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Autofac.Configuration.Util;

namespace Autofac.Configuration.Elements
{
    [TypeConverter(typeof(ListElementTypeConverter))]
    public class ListElementCollection : ConfigurationElementCollection<ListItemElement>
    {
        public ListElementCollection() : base("item") { }

        private class ListElementTypeConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context,
                                             CultureInfo culture,
                                             object value,
                                             Type destinationType)
            {
                var instantiableType = GetInstantiableType(destinationType);
                var listElementCollection = value as ListElementCollection;
                if (listElementCollection != null && instantiableType != null)
                {
                    var genericArguments = instantiableType.GetGenericArguments();
                    var list = (IList) Activator.CreateInstance(instantiableType);
                    foreach (var current in listElementCollection)
                    {
                        list.Add(TypeManipulation.ChangeToCompatibleType(current.Value, genericArguments[0], null));
                    }
                    return list;
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return GetInstantiableType(destinationType) != null || base.CanConvertTo(context, destinationType);
            }

            private static Type GetInstantiableType(Type destinationType)
            {
                if (typeof(IEnumerable).IsAssignableFrom(destinationType))
                {
                    var array = destinationType.IsGenericType
                                    ? destinationType.GetGenericArguments()
                                    : new[]
                                    {
                                        typeof(object)
                                    };
                    if (array.Length != 1)
                    {
                        return null;
                    }
                    var type = typeof(List<>).MakeGenericType(array);
                    if (destinationType.IsAssignableFrom(type))
                    {
                        return type;
                    }
                }
                return null;
            }
        }
    }
}