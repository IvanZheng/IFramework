using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.MessageQueue.Client.Abstracts;
using Microsoft.Extensions.Logging;
using Org.Apache.Rocketmq;

namespace IFramework.MessageQueue.RocketMQ
{
    public delegate void OnRocketMQMessageReceived(RocketMQConsumer consumer, MessageView message, CancellationToken cancellationToken);

    public class RocketMQConsumer : MessageConsumer
    {
        private readonly ClientConfig _consumerConfiguration;
        private readonly OnRocketMQMessageReceived _onMessageReceived;
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
            var filterExpression = new FilterExpression("*");
            foreach (var topic in Topics)
            {
                subscription.Add(topic, filterExpression);
            }

            // In most case, you don't need to create too many consumers, single pattern is recommended.
            _consumer = new SimpleConsumer.Builder()
                        .SetClientConfig(_consumerConfiguration)
                        .SetConsumerGroup(GroupId)
                        .SetAwaitDuration(TimeSpan.FromSeconds(5))
                        .SetSubscriptionExpression(subscription)
                        .Build()
                        .Result;
            base.Start();
        }

        protected override void PollMessages(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"{Id} start receive {string.Join(",", Topics)}");
            var consumeResult = _consumer.Receive(50, TimeSpan.FromSeconds(15)).Result;
            Logger.LogDebug($"{Id} end receive {string.Join(",", Topics)}");
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
                    Logger.LogDebug($"{Id} consume message: {message.Topic}.{message.MessageQueue.QueueId}.{message.Offset} {message.Tag} {message.MessageId}");
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

        protected override void FinishConsumingMessage(MessageOffset messageOffset)
        {
            base.FinishConsumingMessage(messageOffset);
            _consumer.Ack(messageOffset.GetMessage() as MessageView)
                     .ContinueWith(t =>
                     {
                         if (t.IsFaulted)
                         {
                             Logger.LogError($"{Id} ack failed! topic: {messageOffset.Topic} partition: {messageOffset.Partition} offset: {messageOffset.Offset}");
                         }
                         else
                         {
                             var messageView = messageOffset.GetMessage() as MessageView;
                             Logger.LogDebug($"{Id} Finish consuming topic: {messageView?.Topic} messageType: {messageView?.Tag} saga: {messageView?.Properties.TryGetValue("SagaInfo")} partition: {messageView?.MessageQueue.QueueId} offset: {messageView?.Offset}");
                         }
                     });
        }


        public override Task CommitOffsetAsync(string broker, string topic, int partition, long offset, object queueMessage)
        {
            return Task.CompletedTask;
        }
    }
}