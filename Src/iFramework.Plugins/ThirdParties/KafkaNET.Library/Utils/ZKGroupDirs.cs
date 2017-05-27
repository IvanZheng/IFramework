namespace Kafka.Client.Utils
{
    internal class ZKGroupDirs
    {
        public ZKGroupDirs(string group)
        {
            ConsumerGroupDir = ConsumerDir + "/" + group;
            ConsumerRegistryDir = ConsumerGroupDir + "/ids";
        }

        public string ConsumerDir { get; } = "/consumers";

        public string ConsumerGroupDir { get; }

        public string ConsumerRegistryDir { get; }
    }
}