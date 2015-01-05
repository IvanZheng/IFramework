using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    public class MockEventSubscriber : IMessageConsumer
    {
        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        public string GetStatus()
        {
            return string.Empty;
        }

        public decimal MessageCount
        {
            get { return 0; }
        }
    }
}
