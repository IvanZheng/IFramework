using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Autofac.Configuration.Elements
{
    public class ServiceElement : ConfigurationElement
    {
        private const string TypeAttributeName = "type";
        private const string NameAttributeName = "name";
        internal const string Key = "type";

        [ConfigurationProperty("type", IsRequired = true)]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Type => (string) base["type"];

        [ConfigurationProperty("name", IsRequired = false)]
        public string Name => (string) base["name"];
    }
}