using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
namespace Autofac.Configuration
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"), DebuggerNonUserCode, CompilerGenerated]
	internal class ConfigurationSettingsReaderResources
	{
		private static ResourceManager resourceMan;
		private static CultureInfo resourceCulture;
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(ConfigurationSettingsReaderResources.resourceMan, null))
				{
					ResourceManager resourceManager = new ResourceManager("Autofac.Configuration.ConfigurationSettingsReaderResources", typeof(ConfigurationSettingsReaderResources).Assembly);
					ConfigurationSettingsReaderResources.resourceMan = resourceManager;
				}
				return ConfigurationSettingsReaderResources.resourceMan;
			}
		}
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return ConfigurationSettingsReaderResources.resourceCulture;
			}
			set
			{
				ConfigurationSettingsReaderResources.resourceCulture = value;
			}
		}
		internal static string ArgumentMayNotBeEmpty
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("ArgumentMayNotBeEmpty", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string ConfigurationFileNotFound
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("ConfigurationFileNotFound", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string InitializeSectionHandler
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("InitializeSectionHandler", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string NoXmlInConfiguration
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("NoXmlInConfiguration", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string SectionNotFound
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("SectionNotFound", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string ServiceTypeMustBeSpecified
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("ServiceTypeMustBeSpecified", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string TypeConversionUnsupported
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("TypeConversionUnsupported", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string TypeConverterAttributeTypeNotConverter
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("TypeConverterAttributeTypeNotConverter", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string TypeNotFound
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("TypeNotFound", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string UnrecognisedAutoActivate
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("UnrecognisedAutoActivate", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string UnrecognisedInjectProperties
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("UnrecognisedInjectProperties", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string UnrecognisedOwnership
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("UnrecognisedOwnership", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		internal static string UnrecognisedScope
		{
			get
			{
				return ConfigurationSettingsReaderResources.ResourceManager.GetString("UnrecognisedScope", ConfigurationSettingsReaderResources.resourceCulture);
			}
		}
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal ConfigurationSettingsReaderResources()
		{
		}
	}
}
