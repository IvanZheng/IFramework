using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;

namespace IFramework.MessageQueueCore.RabbitMQ
{
    public class QueueProducer : IMessageProducer
    {
        private readonly string _queue;
        public QueueProducer(string queue)
        {
            _queue = queue;
          
        }

        public void Stop()
        {
        }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}