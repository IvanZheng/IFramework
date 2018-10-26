using IFramework.Message;

namespace IFramework.Event.Impl
{
    public class MockEventSubscriber : IMessageProcessor
    {
        public void Start() { }

        public void Stop() { }

        public string GetStatus()
        {
            return string.Empty;
        }

        public decimal MessageCount => 0;
    }
}