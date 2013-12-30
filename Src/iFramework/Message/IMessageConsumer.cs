using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public delegate void MessageHandledHandler(MessageReply reply);

    public interface IMessageConsumer
    {
        void StartConsuming();
        void PushMessageContext(IMessageContext messageContext);
        string GetStatus();
        event MessageHandledHandler MessageHandled;
        decimal MessageCount { get; }
    }
}
