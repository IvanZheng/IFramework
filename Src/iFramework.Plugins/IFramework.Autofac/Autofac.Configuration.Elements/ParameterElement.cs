using System;
using System.Configuration;
namespace Autofac.Configuration.Elements
{
	public class ParameterElement : ConfigurationElement
	{
		private const string NameAttributeName = "name";
		private const string ValueAttributeName = "value";
		private const string ListElementName = "list";
		private const string DictionaryElementName = "dictionary";
		internal const string Key = "name";
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name
		{
			get
			{
				return (string)base["name"];
			}
		}
		[ConfigurationProperty("value", IsRequired = false)]
		public string Value
		{
			get
			{
				return (string)base["value"];
			}
		}
		[ConfigurationProperty("list", IsRequired = false, DefaultValue = null)]
		public ListElementCollection List
		{
			get
			{
				return base["list"] as ListElementCollection;
			}
		}
		[ConfigurationProperty("dictionary", IsRequired = false, DefaultValue = null)]
		public DictionaryElementCollection Dictionary
		{
			get
			{
				return base["dictionary"] as DictionaryElementCollection;
			}
		}
		public object CoerceValue()
		{
			if (this.List.ElementInformation.IsPresent)
			{
				return this.List;
			}
			if (this.Dictionary.ElementInformation.IsPresent)
			{
				return this.Dictionary;
			}
			return this.Value;
		}
	}
}
