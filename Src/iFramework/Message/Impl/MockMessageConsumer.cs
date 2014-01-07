using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    public class MockMessageConsumer : IMessageConsumer
    {
        public void StartConsuming()
        {
            throw new NotImplementedException();
        }

        public void PushMessageContext(IMessageContext messageContext)
        {
            throw new NotImplementedException();
        }

        public string GetStatus()
        {
            throw new NotImplementedException();
        }

        public event MessageHandledHandler MessageHandled;

        public decimal MessageCount
        {
            get { return 0; }
        }
    }
}
