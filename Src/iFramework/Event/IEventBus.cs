using IFramework.Bus;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IEventBus : IBus<IEvent>
    {
        void PublishAnyway(params IEvent[] events);
        IEnumerable<IEvent> GetMessages();
        IEnumerable<IEvent> GetToPublishAnywayMessages();
        void ClearMessages();
    }
}
