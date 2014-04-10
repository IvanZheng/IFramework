using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event
{
    public interface IEventPublisher
    {
        void Publish(params IMessageContext[] events);
    }
}
