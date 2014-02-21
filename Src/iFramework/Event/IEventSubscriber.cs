using IFramework.Message;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IEventSubscriber<in TEvent> :
        IMessageHandler<TEvent> where TEvent : class, IEvent
    {
    }
}
