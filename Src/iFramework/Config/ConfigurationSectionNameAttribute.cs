using System;

namespace IFramework.Config
{
    public class ConfigurationSectionNameAttribute : Attribute
    {
        public ConfigurationSectionNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}