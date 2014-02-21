using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    class MockEventPublisher : IEventPublisher
    {
        public IEnumerable<Message.IMessageContext> Publish(params IEvent[] events)
        {
            return null;
        }
    }
}
