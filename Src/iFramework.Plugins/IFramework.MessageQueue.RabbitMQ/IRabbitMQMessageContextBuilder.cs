using IFramework.Message;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue.RabbitMQ
{
    public interface IRabbitMQMessageContextBuilder: IMessageContextBuilder
    {
        IMessageContext Build(BasicDeliverEventArgs consumeResult);
    }
}
