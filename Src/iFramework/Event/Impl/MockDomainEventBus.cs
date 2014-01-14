using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    public class MockDomainEventBus : IDomainEventBus
    {
        public void Commit()
        {
            
        }

        public void Dispose()
        {
           
        }


        public IEnumerable<IMessageContext> GetMessageContexts()
        {
            return null;
        }


        public void Publish<TMessage>(TMessage @event) where TMessage : IDomainEvent
        {
            
        }

        public void Publish<TMessage>(IEnumerable<TMessage> events) where TMessage : IDomainEvent
        {
        }
    }
}
