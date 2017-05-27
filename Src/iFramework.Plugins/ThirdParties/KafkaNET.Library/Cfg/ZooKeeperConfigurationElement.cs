using System.Configuration;

namespace Kafka.Client.Cfg
{
    public class ZooKeeperConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("addressList")]
        public string AddressList
        {
            get => (string) this["addressList"];
            set => this["addressList"] = value;
        }

        [ConfigurationProperty("sessionTimeout", IsRequired = false,
            DefaultValue = ZooKeeperConfiguration.DefaultSessionTimeout)]
        public int SessionTimeout => (int) this["sessionTimeout"];

        [ConfigurationProperty("connectionTimeout", IsRequired = false,
            DefaultValue = ZooKeeperConfiguration.DefaultConnectionTimeout)]
        public int ConnectionTimeout => (int) this["connectionTimeout"];

        [ConfigurationProperty("syncTime", IsRequired = false, DefaultValue = ZooKeeperConfiguration.DefaultSyncTime)]
        public int SyncTime => (int) this["syncTime"];

        [ConfigurationProperty("servers", IsRequired = false, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ZooKeeperServerConfigurationElementCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ZooKeeperServerConfigurationElementCollection Servers =>
            (ZooKeeperServerConfigurationElementCollection) this["servers"];
    }
}