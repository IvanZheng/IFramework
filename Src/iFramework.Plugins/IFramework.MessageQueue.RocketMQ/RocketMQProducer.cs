using System;
using System.Linq;
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
using Org.Apache.Rocketmq;

namespace IFramework.MessageQueue.RocketMQ
{
    public class RocketMQProducer : IMessageProducer
    {
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(typeof(RocketMQProducer));

        private readonly Producer _producer;
        private readonly string[] _topics;

        public RocketMQProducer(string endpoints, string[] topics, ProducerConfig config = null)
        {
            config ??= new ProducerConfig();
            _topics = topics.Select(topic => Configuration.Instance
                                                          .FormatMessageQueueName(topic))
                            .ToArray();
            // Credential provider is optional for client configuration.
            var configBuilder = new ClientConfig.Builder()
                                                .SetEndpoints(endpoints)
                                                .EnableSsl(config.Get<bool>("EnableSsl"));

            var accessKey = config["AccessKey"]?.ToString();
            var secretKey = config["SecretKey"]?.ToString();

            if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var credentialsProvider = new StaticSessionCredentialsProvider(accessKey, secretKey);
                configBuilder.SetCredentialsProvider(credentialsProvider);
            }

            var clientConfig = configBuilder.Build();

            // In most case, you don't need to create too many producers, single pattern is recommended.
            // Producer here will be closed automatically.
            _producer = new Producer.Builder()
                        // Set the topic name(s), which is optional but recommended.
                        // It makes producer could prefetch the topic route before message publishing.
                        .SetTopics(topics)
                        .SetClientConfig(clientConfig)
                        .Build()
                        .Result;
        }

        public void Stop()
        {
            _producer?.Dispose();
        }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var rocketMQMessageContext = (MessageContext)messageContext;
            var message = rocketMQMessageContext.PayloadMessage;
            var topic = Configuration.Instance.FormatMessageQueueName(messageContext.Topic);
            return SendAsync(topic, messageContext.Key ?? messageContext.MessageId, rocketMQMessageContext.MessageType, message, cancellationToken);
        }

        private byte[] GetPayloadBytes(object payload)
        {
            var str = payload as string ?? payload.ToJson(processDictionaryKeys: false);
            return Encoding.UTF8.GetBytes(str);
        }

        public async Task SendAsync(string topic, string key, string messageType, PayloadMessage payloadMessage, CancellationToken cancellationToken)
        {
            var retryTimes = 0;

            var bytes = GetPayloadBytes(payloadMessage.Payload);
            var tag = messageType;
            var message = new Org.Apache.Rocketmq.Message.Builder()
                          .SetTopic(topic)
                          .SetBody(bytes)
                          .AddProperties(payloadMessage.Headers)
                          .SetTag(tag)
                          // You could set multiple keys for the single message actually.
                          .SetKeys(key)
                          .Build();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retryTimes++;
                // 每次发送失败后线性增长等待发送时间 如: 5s, 10s, 15s, 20s .... max:5 minutes
                var waitTime = Math.Min(retryTimes * 1000 * 5, 60000 * 5);
                try
                {
                    _logger.LogInformation($"Send message start, {message.Tag}");
                    var result = await _producer.Send(message)
                                                .ConfigureAwait(false);
                    _logger.LogInformation($"Send message end, {message.Tag} messageId={result.MessageId} ");
                    return;
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