using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Message;

namespace IFramework.MessageQueue.RocketMQ
{
    public interface IRocketMQMessageContextBuilder : IMessageContextBuilder
    {
        IMessageContext Build(object pullResult);
    }
}
