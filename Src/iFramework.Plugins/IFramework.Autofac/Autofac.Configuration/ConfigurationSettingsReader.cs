using System;
using System.Configuration;
using System.Globalization;
using Autofac.Configuration.Core;

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
                throw new ArgumentNullException("sectionName");
            SectionHandler = (SectionHandler) ConfigurationManager.GetSection(sectionName);
            if (SectionHandler == null)
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                    ConfigurationSettingsReaderResources.SectionNotFound, sectionName));
        }

        public ConfigurationSettingsReader(string sectionName, string configurationFile)
        {
            SectionHandler = SectionHandler.Deserialize(configurationFile, sectionName);
        }
    }
}