using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.RocketMQ
{
    public class DefaultRocketMQMessageContextBuilder: IRocketMQMessageContextBuilder
    {
        public IMessageContext Build(object messageExt)
        {
            throw new NotImplementedException();
            //return new MessageContext(messageExt.BodyString.ToJsonObject<PayloadMessage>(processDictionaryKeys:false),
            //                          messageExt.Topic,
            //                          messageExt.QueueId,
            //                          messageExt.QueueOffset);
        }
    }
}
