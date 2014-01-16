using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageQueue.ZeroMQ
{
    public interface IInProcMessageConsumer : IMessageConsumer
    {
        void EnqueueMessage(object message);
    }
}
