using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class RabbitMQProducer: IMessageProducer
    {
        private readonly IChannel _channel;
        private readonly string _exchange;
        private readonly string _topic;
        private readonly BasicProperties _properties;
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<RabbitMQProducer>();

        public RabbitMQProducer(IChannel channel, string exchange, string topic, ProducerConfig config = null)
        {
            _channel = channel;
            _exchange = exchange;
            _topic = topic;
            _properties = new BasicProperties { Persistent = true };
        }

        public void Stop()
        {
            _channel.Dispose();
        }

        public async Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var message = ((MessageContext)messageContext).PayloadMessage;
            try
            {
                await _channel.BasicPublishAsync(_exchange, _topic, true, _properties, Encoding.UTF8.GetBytes(message.ToJson(processDictionaryKeys:false)), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "send message failed");
                throw;
            }
        }
    }
}
