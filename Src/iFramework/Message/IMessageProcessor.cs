namespace IFramework.Message
{
    public interface IMessageProcessor
    {
        decimal MessageCount { get; }
        void Start();
        void Stop();
        string GetStatus();
    }
}