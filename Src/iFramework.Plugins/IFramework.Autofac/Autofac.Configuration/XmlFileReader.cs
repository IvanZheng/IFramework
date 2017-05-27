using System;
using System.Globalization;
using Autofac.Configuration.Core;

namespace Autofac.Configuration
{
    public class XmlFileReader : ConfigurationModule
    {
        public XmlFileReader(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture,
                        ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, "fileName"), "fileName");
            SectionHandler = SectionHandler.Deserialize(fileName);
        }
    }
}