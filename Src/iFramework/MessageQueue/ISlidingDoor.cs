namespace IFramework.MessageQueue
{
    public interface ISlidingDoor
    {
        int MessageCount { get; }
        void AddOffset(long offset);
        void RemoveOffset(long offset);
    }
}