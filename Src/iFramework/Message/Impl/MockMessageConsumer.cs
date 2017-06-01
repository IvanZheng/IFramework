using System;

namespace IFramework.Message.Impl
{
    public class MockMessageConsumer : IMessageConsumer
    {
        public void Start() { }

        public string GetStatus()
        {
            throw new NotImplementedException();
        }

        public decimal MessageCount => 0;


        public void Stop() { }

        public void EnqueueMessage(object message) { }
    }
}