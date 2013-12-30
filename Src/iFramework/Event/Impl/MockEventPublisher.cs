using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    class MockEventPublisher : IEventPublisher
    {
        public void Publish(IEnumerable<object> events)
        {
            
        }

        public void Publish(object @event)
        {
        }
    }
}
