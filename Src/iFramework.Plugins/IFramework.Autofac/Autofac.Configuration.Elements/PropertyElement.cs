using System.Configuration;

namespace Autofac.Configuration.Elements
{
    public class PropertyElement : ConfigurationElement
    {
        private const string NameAttributeName = "name";
        private const string ValueAttributeName = "value";
        private const string ListElementName = "list";
        private const string DictionaryElementName = "dictionary";
        internal const string Key = "name";

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name => (string) base["name"];

        [ConfigurationProperty("value", IsRequired = false)]
        public string Value => (string) base["value"];

        [ConfigurationProperty("list", IsRequired = false, DefaultValue = null)]
        public ListElementCollection List => base["list"] as ListElementCollection;

        [ConfigurationProperty("dictionary", IsRequired = false, DefaultValue = null)]
        public DictionaryElementCollection Dictionary => base["dictionary"] as DictionaryElementCollection;

        public object CoerceValue()
        {
            if (List.ElementInformation.IsPresent)
            {
                return List;
            }
            if (Dictionary.ElementInformation.IsPresent)
            {
                return Dictionary;
            }
            return Value;
        }
    }
}