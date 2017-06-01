using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Autofac.Configuration.Elements;

namespace Autofac.Configuration
{
    public class SectionHandler : ConfigurationSection
    {
        private const string ModulesPropertyName = "modules";
        private const string ComponentsPropertyName = "components";
        private const string DefaultAssemblyPropertyName = "defaultAssembly";
        private const string FilesPropertyName = "files";
        public const string DefaultSectionName = "autofac";

        [ConfigurationProperty("components", IsRequired = false)]
        public ComponentElementCollection Components => (ComponentElementCollection) base["components"];

        [TypeConverter(typeof(AssemblyNameConverter))]
        [ConfigurationProperty("defaultAssembly", IsRequired = false)]
        public virtual Assembly DefaultAssembly => (Assembly) base["defaultAssembly"];

        [ConfigurationProperty("files", IsRequired = false)]
        public FileElementCollection Files => (FileElementCollection) base["files"];

        [ConfigurationProperty("modules", IsRequired = false)]
        public ModuleElementCollection Modules => (ModuleElementCollection) base["modules"];

        public static SectionHandler Deserialize(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            reader.MoveToContent();
            if (reader.EOF)
            {
                throw new ConfigurationErrorsException(ConfigurationSettingsReaderResources.NoXmlInConfiguration);
            }
            var sectionHandler = new SectionHandler();
            sectionHandler.DeserializeElement(reader, false);
            return sectionHandler;
        }

        public static SectionHandler Deserialize(string configurationFile)
        {
            return Deserialize(configurationFile, null);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification =
            "The XmlTextReader disposal will automatically dispose the stream.")]
        public static SectionHandler Deserialize(string configurationFile, string configurationSection)
        {
            if (string.IsNullOrWhiteSpace(configurationSection))
            {
                configurationSection = "autofac";
            }
            configurationFile = NormalizeConfigurationFilePath(configurationFile);
            var exeConfigurationFileMap = new ExeConfigurationFileMap();
            exeConfigurationFileMap.ExeConfigFilename = configurationFile;
            System.Configuration.Configuration configuration = null;
            try
            {
                configuration =
                    ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap,
                                                                    ConfigurationUserLevel.None);
            }
            catch (ConfigurationErrorsException)
            {
                using (var xmlTextReader = new XmlTextReader(File.OpenRead(configurationFile)))
                {
                    var result = Deserialize(xmlTextReader);
                    return result;
                }
            }
            var sectionHandler = (SectionHandler) configuration.GetSection(configurationSection);
            if (sectionHandler == null)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                                                                     ConfigurationSettingsReaderResources.SectionNotFound, configurationSection));
            }
            return sectionHandler;
        }

        private static string NormalizeConfigurationFilePath(string configurationFile)
        {
            if (configurationFile == null)
            {
                throw new ArgumentNullException("configurationFile");
            }
            if (configurationFile.Length == 0)
            {
                throw new ArgumentException(
                                            string.Format(CultureInfo.CurrentCulture,
                                                          ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, "configurationFile"),
                                            "configurationFile");
            }
            if (!Path.IsPathRooted(configurationFile))
            {
                var directoryName = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                configurationFile = Path.Combine(directoryName, configurationFile);
            }
            if (!File.Exists(configurationFile))
            {
                throw new FileNotFoundException(ConfigurationSettingsReaderResources.ConfigurationFileNotFound,
                                                configurationFile);
            }
            return configurationFile;
        }
    }
}