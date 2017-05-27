namespace Autofac.Configuration.Elements
{
    public class MetadataElementCollection : NamedConfigurationElementCollection<MetadataElement>
    {
        public MetadataElementCollection() : base("item", "name")
        {
        }
    }
}