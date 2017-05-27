using System.Configuration;

namespace IFramework.Config
{
    public class HandlerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get => (string) base["name"];
            set => base["name"] = value;
        }

        [ConfigurationProperty("sourceType", IsRequired = true)]
        public HandlerSourceType SourceType
        {
            get => (HandlerSourceType) base["sourceType"];
            set => base["sourceType"] = value;
        }

        [ConfigurationProperty("source", IsRequired = true)]
        public string Source
        {
            get => (string) base["source"];
            set => base["source"] = value;
        }
    }
}