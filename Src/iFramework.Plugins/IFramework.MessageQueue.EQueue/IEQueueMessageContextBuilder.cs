using System;
using System.Collections.Generic;
using System.Text;
using EQueue.Protocols;
using IFramework.Message;

namespace IFramework.MessageQueue.EQueue
{
    public interface IEQueueMessageContextBuilder:IMessageContextBuilder
    {
        IMessageContext Build(QueueMessage message);
    }
}
