using Autofac.Configuration.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
namespace Autofac.Configuration.Elements
{
	[TypeConverter(typeof(DictionaryElementCollection.DictionaryElementTypeConverter))]
	public class DictionaryElementCollection : ConfigurationElementCollection<ListItemElement>
	{
		private class DictionaryElementTypeConverter : TypeConverter
		{
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				Type instantiableType = DictionaryElementCollection.DictionaryElementTypeConverter.GetInstantiableType(destinationType);
				DictionaryElementCollection dictionaryElementCollection = value as DictionaryElementCollection;
				if (dictionaryElementCollection != null && instantiableType != null)
				{
					IDictionary dictionary = (IDictionary)Activator.CreateInstance(instantiableType);
					Type[] genericArguments = instantiableType.GetGenericArguments();
					foreach (ListItemElement current in dictionaryElementCollection)
					{
						if (string.IsNullOrEmpty(current.Key))
						{
							throw new ConfigurationErrorsException("Key cannot be null in a dictionary element.");
						}
						object key = TypeManipulation.ChangeToCompatibleType(current.Key, genericArguments[0], null);
						object value2 = TypeManipulation.ChangeToCompatibleType(current.Value, genericArguments[1], null);
						dictionary.Add(key, value2);
					}
					return dictionary;
				}
				return base.ConvertTo(context, culture, value, destinationType);
			}
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			{
				return DictionaryElementCollection.DictionaryElementTypeConverter.GetInstantiableType(destinationType) != null || base.CanConvertTo(context, destinationType);
			}
			private static Type GetInstantiableType(Type destinationType)
			{
				if (typeof(IDictionary).IsAssignableFrom(destinationType) || (destinationType.IsGenericType && typeof(IDictionary<, >).IsAssignableFrom(destinationType.GetGenericTypeDefinition())))
				{
					Type[] array = destinationType.IsGenericType ? destinationType.GetGenericArguments() : new Type[]
					{
						typeof(string),
						typeof(object)
					};
					if (array.Length != 2)
					{
						return null;
					}
					Type type = typeof(Dictionary<, >).MakeGenericType(array);
					if (destinationType.IsAssignableFrom(type))
					{
						return type;
					}
				}
				return null;
			}
		}
		public DictionaryElementCollection() : base("item")
		{
		}
	}
}
