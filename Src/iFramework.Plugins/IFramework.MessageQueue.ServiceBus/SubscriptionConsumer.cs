using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Message;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using Microsoft.ServiceBus.Messaging;
using MessageState = Microsoft.ServiceBus.Messaging.MessageState;

namespace IFramework.MessageQueue.ServiceBus
{
    public class SubscriptionConsumer : ServiceBusConsumer
    {
        private readonly SubscriptionClient _subscriptionClient;

        public SubscriptionConsumer(string id,
                                    OnMessagesReceived onMessagesReceived,
                                    SubscriptionClient subscriptionClient)
            : base(id, onMessagesReceived)
        {
            _subscriptionClient = subscriptionClient;
        }

        public override void CommitOffset(IMessageContext messageContext)
        {
            var sequenceNumber = messageContext.Offset;
            try
            {
                var toCompleteMessage = _subscriptionClient.Receive(sequenceNumber);
                toCompleteMessage.Complete();
            }
            catch (Exception ex)
            {
                _logger.Error($"queueClient({Id}) commit offset {sequenceNumber} failed", ex);
            }
        }

        public override void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew(cs => ReceiveTopicMessages(cs as CancellationTokenSource,
                                                                        _onMessagesReceived),
                                             _cancellationTokenSource,
                                             _cancellationTokenSource.Token,
                                             TaskCreationOptions.LongRunning,
                                             TaskScheduler.Default);
        }

        private void ReceiveTopicMessages(CancellationTokenSource cancellationSource,
                                          OnMessagesReceived onMessagesReceived)
        {
            var needPeek = true;
            long sequenceNumber = 0;
            IEnumerable<BrokeredMessage> brokeredMessages = null;

            #region peek messages that not been consumed since last time

            while (!cancellationSource.IsCancellationRequested && needPeek)
            {
                try
                {
                    brokeredMessages = _subscriptionClient.PeekBatch(sequenceNumber, 50);
                    if (brokeredMessages == null || brokeredMessages.Count() == 0)
                    {
                        break;
                    }
                    var messageContexts = new List<IMessageContext>();
                    foreach (var message in brokeredMessages)
                    {
                        if (message.State != MessageState.Deferred)
                        {
                            needPeek = false;
                            break;
                        }
                        messageContexts.Add(new MessageContext(message));
                        sequenceNumber = message.SequenceNumber + 1;
                    }
                    onMessagesReceived(messageContexts.ToArray());
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _logger.Error($"subscriptionClient.ReceiveBatch {_subscriptionClient.Name} failed", ex);
                }
            }

            #endregion

            #region receive messages to enqueue consuming queue

            while (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    brokeredMessages =
                        _subscriptionClient.ReceiveBatch(50,
                                                         Configuration.Instance.GetMessageQueueReceiveMessageTimeout());
                    foreach (var message in brokeredMessages)
                    {
                        message.Defer();
                        onMessagesReceived(new MessageContext(message));
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _logger.Error($"subscriptionClient.ReceiveBatch {_subscriptionClient.Name} failed", ex);
                }
            }

            #endregion
        }
    }
}