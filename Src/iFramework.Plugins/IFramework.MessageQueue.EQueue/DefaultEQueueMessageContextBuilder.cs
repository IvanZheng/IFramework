using System;
using System.Collections.Generic;
using System.Text;
using EQueue.Protocols;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.EQueue
{
    public class DefaultEQueueMessageContextBuilder:IEQueueMessageContextBuilder
    {
        public IMessageContext Build(QueueMessage message)
        {
            var equeueMessage = Encoding.UTF8
                                        .GetString(message.Body)
                                        .ToJsonObject<PayloadMessage>(processDictionaryKeys: false);
            return new MessageContext(equeueMessage,
                                      new MessageOffset(message.BrokerName, message.Topic, message.QueueId, message.QueueOffset));
        }
    }
}
