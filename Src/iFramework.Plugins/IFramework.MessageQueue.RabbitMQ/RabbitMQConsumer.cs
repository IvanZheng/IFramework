using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IFramework.MessageQueue.RabbitMQ
{
    public delegate void OnRabbitMQMessageReceived(RabbitMQConsumer consumer, BasicDeliverEventArgs deliverEventArgs);

    public class RabbitMQConsumer:IMessageConsumer
    {
        private readonly IModel _channel;
        private readonly string _topic;
        private readonly string _groupId;
        private readonly OnRabbitMQMessageReceived _onMessageReceived;

        public RabbitMQConsumer(IModel channel, 
                                string topic,
                                string groupId,
                                string consumerId,
                                OnRabbitMQMessageReceived onMessageReceived,
                                ConsumerConfig consumerConfig = null)
        {
            _channel = channel;
            _topic = topic;
            _groupId = groupId;
            _onMessageReceived = onMessageReceived;

            Id = $"{topic}.{groupId}.{consumerId}";
        }

        public void CommitOffset(IMessageContext messageContext)
        {
            _channel.BasicAck((ulong)messageContext.MessageOffset.Offset, false);
        }

        public string Id { get; }
        public void Start()
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += (model, ea) =>
            {
                _onMessageReceived(this, ea);
            };
            _channel.BasicConsume(queue: _groupId,
                                  autoAck: false,
                                  consumer: consumer);

        }

        public void Stop()
        {
            _channel.Dispose();
        }
    }
}
