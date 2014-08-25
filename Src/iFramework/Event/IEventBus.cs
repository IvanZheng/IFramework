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
        IEnumerable<IEvent> GetMessages();
        void ClearMessages();
    }
}
