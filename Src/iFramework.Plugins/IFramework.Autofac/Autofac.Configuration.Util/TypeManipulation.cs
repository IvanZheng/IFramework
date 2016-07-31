using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace Autofac.Configuration.Util
{
	internal class TypeManipulation
	{
		public static object ChangeToCompatibleType(object value, Type destinationType, ICustomAttributeProvider memberInfo)
		{
			if (destinationType == null)
			{
				throw new ArgumentNullException("destinationType");
			}
			if (value == null)
			{
				if (!destinationType.IsValueType)
				{
					return null;
				}
				return Activator.CreateInstance(destinationType);
			}
			else
			{
				TypeConverter typeConverter;
				if (memberInfo != null)
				{
					TypeConverterAttribute typeConverterAttribute = memberInfo.GetCustomAttributes(typeof(TypeConverterAttribute), true).Cast<TypeConverterAttribute>().FirstOrDefault<TypeConverterAttribute>();
					if (typeConverterAttribute != null && !string.IsNullOrEmpty(typeConverterAttribute.ConverterTypeName))
					{
						typeConverter = TypeManipulation.GetTypeConverterFromName(typeConverterAttribute.ConverterTypeName);
						if (typeConverter.CanConvertFrom(value.GetType()))
						{
							return typeConverter.ConvertFrom(value);
						}
					}
				}
				typeConverter = TypeDescriptor.GetConverter(value.GetType());
				if (typeConverter.CanConvertTo(destinationType))
				{
					return typeConverter.ConvertTo(value, destinationType);
				}
				if (destinationType.IsInstanceOfType(value))
				{
					return value;
				}
				typeConverter = TypeDescriptor.GetConverter(destinationType);
				if (typeConverter.CanConvertFrom(value.GetType()))
				{
					return typeConverter.ConvertFrom(value);
				}
				if (value is string)
				{
					MethodInfo method = destinationType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public);
					if (method != null)
					{
						object[] array = new object[2];
						array[0] = value;
						object[] array2 = array;
						if ((bool)method.Invoke(null, array2))
						{
							return array2[1];
						}
					}
				}
				throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.TypeConversionUnsupported, new object[]
				{
					value.GetType(),
					destinationType
				}));
			}
		}
		private static TypeConverter GetTypeConverterFromName(string converterTypeName)
		{
			Type type = Type.GetType(converterTypeName, true);
			TypeConverter typeConverter = Activator.CreateInstance(type) as TypeConverter;
			if (typeConverter == null)
			{
				throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.TypeConverterAttributeTypeNotConverter, new object[]
				{
					converterTypeName
				}));
			}
			return typeConverter;
		}
	}
}
