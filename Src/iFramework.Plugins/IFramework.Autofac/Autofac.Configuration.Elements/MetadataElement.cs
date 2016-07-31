using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
namespace Autofac.Configuration.Elements
{
	public class MetadataElement : ConfigurationElement
	{
		private const string NameAttributeName = "name";
		private const string ValueAttributeName = "value";
		private const string TypeAttributeName = "type";
		internal const string Key = "name";
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name
		{
			get
			{
				return (string)base["name"];
			}
		}
		[ConfigurationProperty("value", IsRequired = true)]
		public string Value
		{
			get
			{
				return (string)base["value"];
			}
		}
		[ConfigurationProperty("type", IsRequired = false), SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public string Type
		{
			get
			{
				return ((string)base["type"]) ?? typeof(string).FullName;
			}
		}
	}
}
