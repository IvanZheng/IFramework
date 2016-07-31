using Autofac.Configuration.Core;
using System;
using System.Configuration;
using System.Globalization;
namespace Autofac.Configuration
{
	public class ConfigurationSettingsReader : ConfigurationModule
	{
		public ConfigurationSettingsReader() : this("autofac")
		{
		}
		public ConfigurationSettingsReader(string sectionName)
		{
			if (sectionName == null)
			{
				throw new ArgumentNullException("sectionName");
			}
			base.SectionHandler = (SectionHandler)ConfigurationManager.GetSection(sectionName);
			if (base.SectionHandler == null)
			{
				throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.SectionNotFound, new object[]
				{
					sectionName
				}));
			}
		}
		public ConfigurationSettingsReader(string sectionName, string configurationFile)
		{
			base.SectionHandler = SectionHandler.Deserialize(configurationFile, sectionName);
		}
	}
}
