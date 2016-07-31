using Autofac.Configuration.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
namespace Autofac.Configuration.Elements
{
	[TypeConverter(typeof(ListElementCollection.ListElementTypeConverter))]
	public class ListElementCollection : ConfigurationElementCollection<ListItemElement>
	{
		private class ListElementTypeConverter : TypeConverter
		{
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				Type instantiableType = ListElementCollection.ListElementTypeConverter.GetInstantiableType(destinationType);
				ListElementCollection listElementCollection = value as ListElementCollection;
				if (listElementCollection != null && instantiableType != null)
				{
					Type[] genericArguments = instantiableType.GetGenericArguments();
					IList list = (IList)Activator.CreateInstance(instantiableType);
					foreach (ListItemElement current in listElementCollection)
					{
						list.Add(TypeManipulation.ChangeToCompatibleType(current.Value, genericArguments[0], null));
					}
					return list;
				}
				return base.ConvertTo(context, culture, value, destinationType);
			}
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			{
				return ListElementCollection.ListElementTypeConverter.GetInstantiableType(destinationType) != null || base.CanConvertTo(context, destinationType);
			}
			private static Type GetInstantiableType(Type destinationType)
			{
				if (typeof(IEnumerable).IsAssignableFrom(destinationType))
				{
					Type[] array = destinationType.IsGenericType ? destinationType.GetGenericArguments() : new Type[]
					{
						typeof(object)
					};
					if (array.Length != 1)
					{
						return null;
					}
					Type type = typeof(List<>).MakeGenericType(array);
					if (destinationType.IsAssignableFrom(type))
					{
						return type;
					}
				}
				return null;
			}
		}
		public ListElementCollection() : base("item")
		{
		}
	}
}
