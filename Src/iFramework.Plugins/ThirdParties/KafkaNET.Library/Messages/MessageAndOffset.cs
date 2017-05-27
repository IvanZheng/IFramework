namespace Kafka.Client.Messages
{
    public class MessageAndOffset
    {
        public MessageAndOffset(Message message, long offset)
        {
            Message = message;
            MessageOffset = message.Offset;
            ByteOffset = offset;
        }

        public Message Message { get; }

        public long ByteOffset { get; }

        public long MessageOffset { get; }
    }
}