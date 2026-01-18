using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IChannel _channel;
        private readonly string[] _topics;
        private readonly string _groupId;
        private readonly OnRabbitMQMessageReceived _onMessageReceived;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<RabbitMQConsumer>();

        public RabbitMQConsumer(IChannel channel, 
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
            _channel.BasicAckAsync((ulong)messageContext.MessageOffset.Offset, false).GetAwaiter().GetResult();
        }

        public string Id { get; }
        protected AsyncEventingBasicConsumer Consumer;
        public void Start()
        {
            Consumer = new AsyncEventingBasicConsumer(_channel);
            
            Consumer.ReceivedAsync += async (model, ea) =>
            {
                _logger.LogDebug($"consumer({Id}) receive message, routingKey: {ea.RoutingKey} deliveryTag: {ea.DeliveryTag}");
                _onMessageReceived(this, ea, _cancellationTokenSource.Token);
                await Task.CompletedTask;
            };
            _channel.BasicConsumeAsync(queue: _groupId,
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
