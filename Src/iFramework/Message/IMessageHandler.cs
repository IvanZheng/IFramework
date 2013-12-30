using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageHandler
    {
        void Handle(object message);
    }
    public interface IMessageHandler<in TMessage> where TMessage : class
    {
        void Handle(TMessage message);
    }
}
