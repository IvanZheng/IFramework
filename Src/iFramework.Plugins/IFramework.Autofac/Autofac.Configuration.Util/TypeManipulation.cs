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
        public static object ChangeToCompatibleType(object value,
                                                    Type destinationType,
                                                    ICustomAttributeProvider memberInfo)
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
            TypeConverter typeConverter;
            if (memberInfo != null)
            {
                var typeConverterAttribute = memberInfo.GetCustomAttributes(typeof(TypeConverterAttribute), true)
                                                       .Cast<TypeConverterAttribute>()
                                                       .FirstOrDefault();
                if (typeConverterAttribute != null && !string.IsNullOrEmpty(typeConverterAttribute.ConverterTypeName))
                {
                    typeConverter = GetTypeConverterFromName(typeConverterAttribute.ConverterTypeName);
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
                var method = destinationType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    var array = new object[2];
                    array[0] = value;
                    var array2 = array;
                    if ((bool) method.Invoke(null, array2))
                    {
                        return array2[1];
                    }
                }
            }
            throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                                                                 ConfigurationSettingsReaderResources.TypeConversionUnsupported, value.GetType(), destinationType));
        }

        private static TypeConverter GetTypeConverterFromName(string converterTypeName)
        {
            var type = Type.GetType(converterTypeName, true);
            var typeConverter = Activator.CreateInstance(type) as TypeConverter;
            if (typeConverter == null)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                                                                     ConfigurationSettingsReaderResources.TypeConverterAttributeTypeNotConverter, converterTypeName));
            }
            return typeConverter;
        }
    }
}