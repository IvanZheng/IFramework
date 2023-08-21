using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Logging;
using Org.Apache.Rocketmq;

namespace IFramework.MessageQueue.RocketMQ
{
    public delegate void OnRocketMQMessageReceived(RocketMQConsumer consumer, MessageView message, CancellationToken cancellationToken);

    public class RocketMQConsumer : MessageConsumer
    {
        private readonly OnRocketMQMessageReceived _onMessageReceived;
        private readonly ClientConfig _consumerConfiguration;
        private SimpleConsumer _consumer;
        public RocketMQConsumer(string endpoints,
                                string[] topics,
                                string groupId,
                                string consumerId,
                                OnRocketMQMessageReceived onMessageReceived,
                                ConsumerConfig consumerConfig = null)
            : base(topics, groupId, consumerId, consumerConfig)
        {
            _onMessageReceived = onMessageReceived;



            // Credential provider is optional for client configuration.
            var configBuilder = new ClientConfig.Builder()
                .SetEndpoints(endpoints)
                .EnableSsl(ConsumerConfig.Get<bool>("EnableSsl"));

            var accessKey = ConsumerConfig.Get("AccessKey");
            var secretKey = ConsumerConfig.Get("SecretKey");

            if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var credentialsProvider = new StaticSessionCredentialsProvider(accessKey, secretKey);
                configBuilder.SetCredentialsProvider(credentialsProvider);
            }

            _consumerConfiguration = configBuilder.Build();
        }

        public override void Start()
        {
            // Add your subscriptions.
            var subscription = new Dictionary<string, FilterExpression>();
            foreach (var topic in Topics)
            {
                subscription.Add(topic, new FilterExpression("*"));
            }

            // In most case, you don't need to create too many consumers, single pattern is recommended.
            _consumer = new SimpleConsumer.Builder()
                        .SetClientConfig(_consumerConfiguration)
                        .SetConsumerGroup(GroupId)
                        .SetAwaitDuration(TimeSpan.FromSeconds(15))
                        .SetSubscriptionExpression(subscription)
                        .Build().Result;
            base.Start();
        }

        protected override void PollMessages(CancellationToken cancellationToken)
        {
            var consumeResult = _consumer.Receive(50, TimeSpan.FromSeconds(15)).Result;
            if (consumeResult?.Count > 0)
            {
                _consumer_OnMessage(_consumer, consumeResult, cancellationToken);
            }
        }

        private void _consumer_OnMessage(SimpleConsumer sender, List<MessageView> messages, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                try
                {
                    Logger.LogDebug($"consume message: {message.Topic}.{message.MessageQueue.QueueId}.{message.Offset}");
                    AddMessageOffset(message.Topic, message.MessageQueue.QueueId, message.Offset);
                    _onMessageReceived(this, message, cancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "{0} topic:{1} partition:{2} offset:{3} _consumer_OnMessage failed!",
                                    Id, message.Topic, message.MessageQueue.QueueId, message.Offset);
                    if (message.MessageId != null)
                    {
                        FinishConsumingMessage(new MessageOffset(null, message.Topic, message.MessageQueue.QueueId, message.Offset, message));
                    }
                }
            }
            
        }

        public override Task CommitOffsetAsync(string broker, string topic, int partition, long offset, object queueMessage)
        {
            return _consumer.Ack(queueMessage as MessageView);
        }
    }
}