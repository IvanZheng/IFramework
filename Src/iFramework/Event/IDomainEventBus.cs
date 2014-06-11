using IFramework.Bus;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IDomainEventBus : IBus<IDomainEvent>
    {
        IEnumerable<IDomainEvent> GetMessages();
        void ClearMessages();
    }
}
