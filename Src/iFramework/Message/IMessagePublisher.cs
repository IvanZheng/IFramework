using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessagePublisher
    {
        void Start();
        void Stop();
        void Publish(params IMessage[] events);
        void Publish(params IMessageContext[] eventContexts);
    }
}
