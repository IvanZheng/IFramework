using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    class MockMessagePublisher : IMessagePublisher
    {
        public void Send(params IMessage[] messages)
        {
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
        }


        public void Send(params IMessageContext[] messageContexts)
        {
        }
    }
}
