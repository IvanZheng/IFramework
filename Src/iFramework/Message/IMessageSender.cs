using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageSender
    {
        void Start();
        void Stop();
        void Send(params IMessage[] events);
        void Send(params IMessageContext[] eventContexts);
    }
}
