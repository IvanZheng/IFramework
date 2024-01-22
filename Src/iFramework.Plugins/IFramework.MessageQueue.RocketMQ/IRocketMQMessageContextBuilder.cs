using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Message;
using Org.Apache.Rocketmq;

namespace IFramework.MessageQueue.RocketMQ
{
    public interface IRocketMQMessageContextBuilder : IMessageContextBuilder
    {
        IMessageContext Build(MessageView messageView, IMessageTypeProvider messageTypeProvider);
    }
}
