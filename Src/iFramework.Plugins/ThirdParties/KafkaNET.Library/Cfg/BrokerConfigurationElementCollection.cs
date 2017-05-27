using System.Configuration;

namespace Kafka.Client.Cfg
{
    public class BrokerConfigurationElementCollection : ConfigurationElementCollection
    {
        public BrokerConfigurationElement this[int index]
        {
            get => BaseGet(index) as BrokerConfigurationElement;

            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);

                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new BrokerConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BrokerConfigurationElement) element).Id;
        }
    }
}