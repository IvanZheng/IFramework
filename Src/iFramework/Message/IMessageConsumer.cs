namespace IFramework.Message
{
    public interface IMessageConsumer
    {
        decimal MessageCount { get; }
        void Start();
        void Stop();
        string GetStatus();
    }
}