using System.Configuration;

namespace Kafka.Client.Cfg
{
    public class ZooKeeperServerConfigurationElementCollection : ConfigurationElementCollection
    {
        public ZooKeeperServerConfigurationElement this[int index]
        {
            get => BaseGet(index) as ZooKeeperServerConfigurationElement;

            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }

                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ZooKeeperServerConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ZooKeeperServerConfigurationElement) element).Host
                   + ((ZooKeeperServerConfigurationElement) element).Port;
        }
    }
}