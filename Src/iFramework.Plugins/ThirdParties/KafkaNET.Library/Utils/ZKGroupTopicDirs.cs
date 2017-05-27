namespace Kafka.Client.Utils
{
    internal class ZKGroupTopicDirs : ZKGroupDirs
    {
        public ZKGroupTopicDirs(string group, string topic) : base(group)
        {
            ConsumerOffsetDir = ConsumerGroupDir + "/offsets/" + topic;
            ConsumerOwnerDir = ConsumerGroupDir + "/owners/" + topic;
        }

        public string ConsumerOffsetDir { get; }

        public string ConsumerOwnerDir { get; }
    }
}