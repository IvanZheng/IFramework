using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Bus
{
    public interface IBus<in TMessage> : IDisposable
    {
        void Publish(TMessage @event);
        void Publish(IEnumerable<TMessage> events);
    }
}
