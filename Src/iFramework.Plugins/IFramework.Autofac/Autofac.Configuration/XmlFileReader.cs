using Autofac.Configuration.Core;
using System;
using System.Globalization;
namespace Autofac.Configuration
{
	public class XmlFileReader : ConfigurationModule
	{
		public XmlFileReader(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("fileName");
			}
			if (fileName.Length == 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, new object[]
				{
					"fileName"
				}), "fileName");
			}
			base.SectionHandler = SectionHandler.Deserialize(fileName);
		}
	}
}
