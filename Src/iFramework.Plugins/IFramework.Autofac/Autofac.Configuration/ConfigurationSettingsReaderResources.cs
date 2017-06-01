using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Autofac.Configuration
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class ConfigurationSettingsReaderResources
    {
        private static ResourceManager resourceMan;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ConfigurationSettingsReaderResources() { }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(resourceMan, null))
                {
                    var resourceManager =
                        new ResourceManager("Autofac.Configuration.ConfigurationSettingsReaderResources",
                                            typeof(ConfigurationSettingsReaderResources).Assembly);
                    resourceMan = resourceManager;
                }
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture { get; set; }

        internal static string ArgumentMayNotBeEmpty => ResourceManager.GetString("ArgumentMayNotBeEmpty", Culture);

        internal static string ConfigurationFileNotFound => ResourceManager.GetString("ConfigurationFileNotFound",
                                                                                      Culture);

        internal static string InitializeSectionHandler => ResourceManager.GetString("InitializeSectionHandler",
                                                                                     Culture);

        internal static string NoXmlInConfiguration => ResourceManager.GetString("NoXmlInConfiguration", Culture);

        internal static string SectionNotFound => ResourceManager.GetString("SectionNotFound", Culture);

        internal static string ServiceTypeMustBeSpecified => ResourceManager.GetString("ServiceTypeMustBeSpecified",
                                                                                       Culture);

        internal static string TypeConversionUnsupported => ResourceManager.GetString("TypeConversionUnsupported",
                                                                                      Culture);

        internal static string TypeConverterAttributeTypeNotConverter => ResourceManager.GetString(
                                                                                                   "TypeConverterAttributeTypeNotConverter", Culture);

        internal static string TypeNotFound => ResourceManager.GetString("TypeNotFound", Culture);

        internal static string UnrecognisedAutoActivate => ResourceManager.GetString("UnrecognisedAutoActivate",
                                                                                     Culture);

        internal static string UnrecognisedInjectProperties => ResourceManager.GetString("UnrecognisedInjectProperties",
                                                                                         Culture);

        internal static string UnrecognisedOwnership => ResourceManager.GetString("UnrecognisedOwnership", Culture);

        internal static string UnrecognisedScope => ResourceManager.GetString("UnrecognisedScope", Culture);
    }
}