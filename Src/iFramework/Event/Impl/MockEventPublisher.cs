using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    class MockEventPublisher : IEventPublisher
    {
        public IEnumerable<Message.IMessageContext> Publish(IEnumerable<IDomainEvent> events)
        {
            return null;
        }

        public Message.IMessageContext Publish(IDomainEvent @event)
        {
            return null;
        }
    }
}
