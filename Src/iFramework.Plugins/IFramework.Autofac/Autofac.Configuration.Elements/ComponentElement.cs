using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
namespace Autofac.Configuration.Elements
{
	public class ComponentElement : ConfigurationElement
	{
		private const string TypeAttributeName = "type";
		private const string ServiceAttributeName = "service";
		private const string ServicesElementName = "services";
		private const string ParametersElementName = "parameters";
		private const string PropertiesElementName = "properties";
		private const string MetadataElementName = "metadata";
		private const string MemberOfAttributeName = "member-of";
		private const string NameAttributeName = "name";
		private const string InstanceScopeAttributeName = "instance-scope";
		private const string InstanceOwnershipAttributeName = "instance-ownership";
		private const string InjectPropertiesAttributeName = "inject-properties";
		private const string AutoActivateAttibuteName = "auto-activate";
		internal const string Key = "type";
		[ConfigurationProperty("type", IsRequired = true), SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public string Type
		{
			get
			{
				return (string)base["type"];
			}
		}
		[ConfigurationProperty("service", IsRequired = false)]
		public string Service
		{
			get
			{
				return (string)base["service"];
			}
		}
		[ConfigurationProperty("member-of", IsRequired = false)]
		public string MemberOf
		{
			get
			{
				return (string)base["member-of"];
			}
		}
		[ConfigurationProperty("name", IsRequired = false)]
		public string Name
		{
			get
			{
				return (string)base["name"];
			}
		}
		[ConfigurationProperty("instance-scope", IsRequired = false)]
		public string InstanceScope
		{
			get
			{
				return (string)base["instance-scope"];
			}
		}
		[ConfigurationProperty("instance-ownership", IsRequired = false)]
		public string Ownership
		{
			get
			{
				return (string)base["instance-ownership"];
			}
		}
		[ConfigurationProperty("inject-properties", IsRequired = false)]
		public string InjectProperties
		{
			get
			{
				return (string)base["inject-properties"];
			}
		}
		[ConfigurationProperty("auto-activate", IsRequired = false)]
		public string AutoActivate
		{
			get
			{
				return (string)base["auto-activate"];
			}
		}
		[ConfigurationProperty("services", IsRequired = false)]
		public ServiceElementCollection Services
		{
			get
			{
				return (ServiceElementCollection)base["services"];
			}
		}
		[ConfigurationProperty("parameters", IsRequired = false)]
		public ParameterElementCollection Parameters
		{
			get
			{
				return (ParameterElementCollection)base["parameters"];
			}
		}
		[ConfigurationProperty("properties", IsRequired = false)]
		public new PropertyElementCollection Properties
		{
			get
			{
				return (PropertyElementCollection)base["properties"];
			}
		}
		[ConfigurationProperty("metadata", IsRequired = false)]
		public MetadataElementCollection Metadata
		{
			get
			{
				return (MetadataElementCollection)base["metadata"];
			}
		}
	}
}
