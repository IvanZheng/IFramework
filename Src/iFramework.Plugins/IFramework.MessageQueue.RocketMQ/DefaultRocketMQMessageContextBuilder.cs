using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using Org.Apache.Rocketmq;

namespace IFramework.MessageQueue.RocketMQ
{
    public class DefaultRocketMQMessageContextBuilder: IRocketMQMessageContextBuilder
    {
        public IMessageContext Build(MessageView messageExt, IMessageTypeProvider messageTypeProvider)
        {
            var body = Encoding.UTF8.GetString(messageExt.Body);
            var messageType = messageTypeProvider.GetMessageType(messageExt.Tag);
            var message = messageType == null ? body : body.ToJsonObject(messageType, processDictionaryKeys: false);
            return new MessageContext(message, messageExt.Topic, messageExt.MessageQueue.QueueId, messageExt.Offset, messageExt);
        }
    }
}
