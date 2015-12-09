using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    public class MockEventBus : IEventBus
    {
        public void Commit()
        {
            
        }

        public void Dispose()
        {
           
        }

        public void Publish<TMessage>(TMessage @event) where TMessage : IEvent
        {
            
        }

        public void Publish<TMessage>(IEnumerable<TMessage> events) where TMessage : IEvent
        {
        }

        public IEnumerable<IEvent> GetMessages()
        {
            return null;
        }


        public void ClearMessages()
        {
        }

        public void PublishAnyway(params IEvent[] events)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEvent> GetToPublishAnywayMessages()
        {
            throw new NotImplementedException();
        }
    }
}
