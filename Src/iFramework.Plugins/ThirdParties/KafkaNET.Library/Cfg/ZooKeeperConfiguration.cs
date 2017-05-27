namespace Kafka.Client.Cfg
{
    public class ZooKeeperConfiguration
    {
        public const int DefaultSessionTimeout = 6000;

        public const int DefaultConnectionTimeout = 6000;

        public const int DefaultSyncTime = 2000;

        public ZooKeeperConfiguration()
            : this(null, DefaultSessionTimeout, DefaultConnectionTimeout, DefaultSyncTime)
        {
        }

        public ZooKeeperConfiguration(string zkconnect, int zksessionTimeoutMs, int zkconnectionTimeoutMs,
            int zksyncTimeMs)
        {
            ZkConnect = zkconnect;
            ZkConnectionTimeoutMs = zkconnectionTimeoutMs;
            ZkSessionTimeoutMs = zksessionTimeoutMs;
            ZkSyncTimeMs = zksyncTimeMs;
        }

        public string ZkConnect { get; set; }

        public int ZkSessionTimeoutMs { get; set; }

        public int ZkConnectionTimeoutMs { get; set; }

        public int ZkSyncTimeMs { get; set; }
    }
}