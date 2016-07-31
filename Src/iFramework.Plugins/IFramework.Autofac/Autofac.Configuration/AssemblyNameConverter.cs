using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Reflection;
namespace Autofac.Configuration
{
	public class AssemblyNameConverter : ConfigurationConverterBase
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return Assembly.Load(text);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			string result = null;
			if (value != null)
			{
				if (!typeof(Assembly).IsAssignableFrom(value.GetType()))
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.TypeConversionUnsupported, new object[]
					{
						value.GetType(),
						typeof(Assembly)
					}));
				}
				result = ((Assembly)value).FullName;
			}
			return result;
		}
	}
}
