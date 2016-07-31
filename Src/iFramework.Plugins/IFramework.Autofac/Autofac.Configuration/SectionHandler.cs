using Autofac.Configuration.Elements;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
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
		public ComponentElementCollection Components
		{
			get
			{
				return (ComponentElementCollection)base["components"];
			}
		}
		[TypeConverter(typeof(AssemblyNameConverter)), ConfigurationProperty("defaultAssembly", IsRequired = false)]
		public virtual Assembly DefaultAssembly
		{
			get
			{
				return (Assembly)base["defaultAssembly"];
			}
		}
		[ConfigurationProperty("files", IsRequired = false)]
		public FileElementCollection Files
		{
			get
			{
				return (FileElementCollection)base["files"];
			}
		}
		[ConfigurationProperty("modules", IsRequired = false)]
		public ModuleElementCollection Modules
		{
			get
			{
				return (ModuleElementCollection)base["modules"];
			}
		}
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
			SectionHandler sectionHandler = new SectionHandler();
			sectionHandler.DeserializeElement(reader, false);
			return sectionHandler;
		}
		public static SectionHandler Deserialize(string configurationFile)
		{
			return SectionHandler.Deserialize(configurationFile, null);
		}
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The XmlTextReader disposal will automatically dispose the stream.")]
		public static SectionHandler Deserialize(string configurationFile, string configurationSection)
		{
			if (string.IsNullOrWhiteSpace(configurationSection))
			{
				configurationSection = "autofac";
			}
			configurationFile = SectionHandler.NormalizeConfigurationFilePath(configurationFile);
			ExeConfigurationFileMap exeConfigurationFileMap = new ExeConfigurationFileMap();
			exeConfigurationFileMap.ExeConfigFilename = configurationFile;
			System.Configuration.Configuration configuration = null;
			try
			{
				configuration = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
			}
			catch (ConfigurationErrorsException)
			{
				using (XmlTextReader xmlTextReader = new XmlTextReader(File.OpenRead(configurationFile)))
				{
					SectionHandler result = SectionHandler.Deserialize(xmlTextReader);
					return result;
				}
			}
			SectionHandler sectionHandler = (SectionHandler)configuration.GetSection(configurationSection);
			if (sectionHandler == null)
			{
				throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.SectionNotFound, new object[]
				{
					configurationSection
				}));
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
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, new object[]
				{
					"configurationFile"
				}), "configurationFile");
			}
			if (!Path.IsPathRooted(configurationFile))
			{
				string directoryName = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
				configurationFile = Path.Combine(directoryName, configurationFile);
			}
			if (!File.Exists(configurationFile))
			{
				throw new FileNotFoundException(ConfigurationSettingsReaderResources.ConfigurationFileNotFound, configurationFile);
			}
			return configurationFile;
		}
	}
}
