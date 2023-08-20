using IFramework.MessageQueue.Client.Abstracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageQueue.RocketMQ
{
    public class RocketMQProducer: IMessageProducer
    {

        //rivate readonly Producer _producer;
        private readonly ILogger _logger;
        private readonly string _topic;

        public RocketMQProducer(string topic, ILogger<RocketMQProducer> logger) // Producer producer)
        {
            _topic = Configuration.Instance.FormatMessageQueueName(topic);
            //_producer = producer;
            _logger = logger;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var rocketMQMessageContext = (MessageContext)messageContext;
            var message = rocketMQMessageContext.PayloadMessage;
            var topic = Configuration.Instance.FormatMessageQueueName(messageContext.Topic);
            return SendAsync(topic, messageContext.Key ?? messageContext.MessageId, rocketMQMessageContext.MessageType, message, cancellationToken);
        }


        public async Task SendAsync(string topic, string key, string messageType, PayloadMessage message, CancellationToken cancellationToken)
        {
            var retryTimes = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    //var result = await _producer.PublishAsync(message, messageType, key)
                    //                            .ConfigureAwait(false);
                    //if (result.Status != SendStatus.SendOK)
                    //{
                    //    throw new Exception(result.ToString());
                    //}
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"send message failed topic: {topic} key:{key} messageType:{messageType}");
                    await Task.Delay(waitTime, cancellationToken);
                }
            }
            //_logger.Debug($"send message topic: {_topic} Partition: {result.Partition}, Offset: {result.Offset}");
        }
    }
}
