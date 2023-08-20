using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using RabbitMQ.Client.Events;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class DefaultRabbitMQMessageContextBuilder: IRabbitMQMessageContextBuilder
    {
        public IMessageContext Build(BasicDeliverEventArgs args)
        {
            var message = Encoding.UTF8.GetString(args.Body.ToArray()).ToJsonObject<PayloadMessage>(processDictionaryKeys: false);
            return new MessageContext(message, new MessageOffset(string.Empty, args.Exchange, 0, (long)args.DeliveryTag));
        }
    }
}
