using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using IFramework.DependencyInjection;
using IFramework.Message;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IFramework.MessageQueue.RabbitMQ
{
    public delegate void OnRabbitMQMessageReceived(RabbitMQConsumer consumer, BasicDeliverEventArgs deliverEventArgs, CancellationToken cancellationToken);

    public class RabbitMQConsumer:IMessageConsumer
    {
        private readonly IModel _channel;
        private readonly string[] _topics;
        private readonly string _groupId;
        private readonly OnRabbitMQMessageReceived _onMessageReceived;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<RabbitMQConsumer>();

        public RabbitMQConsumer(IModel channel, 
                                string[] topics,
                                string groupId,
                                string consumerId,
                                OnRabbitMQMessageReceived onMessageReceived,
                                ConsumerConfig consumerConfig = null)
        {
            _channel = channel;
            _topics = topics;
            _groupId = groupId;

            _cancellationTokenSource = new CancellationTokenSource();
            _onMessageReceived = onMessageReceived;

            Id = $"{groupId}.{consumerId}";
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            _channel.BasicAck((ulong)messageContext.MessageOffset.Offset, false);
        }

        public string Id { get; }
        protected EventingBasicConsumer Consumer;
        public void Start()
        {
            Consumer = new EventingBasicConsumer(_channel);
            
            Consumer.Received += (model, ea) =>
            {
                _logger.LogDebug($"consumer({Id}) receive message, routingKey: {ea.RoutingKey} deliveryTag: {ea.DeliveryTag}");
                _onMessageReceived(this, ea, _cancellationTokenSource.Token);
            };
            _channel.BasicConsume(queue: _groupId,
                                  autoAck: false,
                                  consumer: Consumer);

        }

        public string Status => $"{Id}:{Consumer}";

        public void Stop()
        {
            _cancellationTokenSource.Cancel(true);
            _channel.Dispose();
        }
    }
}
