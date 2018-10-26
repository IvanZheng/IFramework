using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Bus
{
    public interface IBus<in TMessage> : IDisposable
    {
        void Publish<TTMessage>(TTMessage @event) where TTMessage : TMessage;
        void Publish<TTMessage>(IEnumerable<TTMessage> events) where TTMessage : TMessage;
    }
}
