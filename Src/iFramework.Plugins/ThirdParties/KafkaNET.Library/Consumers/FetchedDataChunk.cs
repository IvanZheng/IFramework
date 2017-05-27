using System;
using Kafka.Client.Messages;

namespace Kafka.Client.Consumers
{
    public class FetchedDataChunk : IEquatable<FetchedDataChunk>
    {
        public FetchedDataChunk(BufferedMessageSet messages, PartitionTopicInfo topicInfo, long fetchOffset)
        {
            Messages = messages;
            TopicInfo = topicInfo;
            FetchOffset = fetchOffset;
        }

        public BufferedMessageSet Messages { get; set; }

        public PartitionTopicInfo TopicInfo { get; set; }

        public long FetchOffset { get; set; }

        public bool Equals(FetchedDataChunk other)
        {
            return Messages == other.Messages &&
                   TopicInfo == other.TopicInfo &&
                   FetchOffset == other.FetchOffset;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FetchedDataChunk;
            if (other == null)
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Messages.GetHashCode() ^ TopicInfo.GetHashCode() ^ FetchOffset.GetHashCode();
        }
    }
}