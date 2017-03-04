using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.SysExceptions;
using IFramework.Command;
using IFramework.IoC;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message.Impl;
using IFramework.Config;
using System.Threading.Tasks;

namespace IFramework.Event.Impl
{
    public class EventSubscriber : IMessageConsumer
    {
        readonly string _topic;
        protected IMessageQueueClient _MessageQueueClient;
        protected ICommandBus _commandBus;
        protected IMessagePublisher _messagePublisher;
        protected IHandlerProvider _handlerProvider;
        protected string _subscriptionName;
        protected string _consumerId;
        protected MessageProcessor _messageProcessor;
        protected ILogger _logger;
        protected ICommitOffsetable _internalConsumer;

        public EventSubscriber(IMessageQueueClient messageQueueClient,
                               IHandlerProvider handlerProvider,
                               ICommandBus commandBus,
                               IMessagePublisher messagePublisher,
                               string subscriptionName,
                               string topic,
                               string consumerId,
                               int mailboxProcessBatchCount = 100)
        {
            _MessageQueueClient = messageQueueClient;
            _handlerProvider = handlerProvider;
            _topic = topic;
            _consumerId = consumerId;
            _subscriptionName = subscriptionName;
            _messagePublisher = messagePublisher;
            _commandBus = commandBus;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(), mailboxProcessBatchCount);
            _logger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType().Name) : null;
        }

        string _producer;
        public string Producer
        {
            get
            {
                return _producer ?? (_producer = $"{_subscriptionName}.{_topic}.{_consumerId}");
            }
        }
        protected void SaveEvent(IMessageContext eventContext)
        {
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            using (var messageStore = scope.Resolve<IMessageStore>())
            {
                messageStore.SaveEvent(eventContext);
            }
        }

        protected async Task ConsumeMessage(IMessageContext eventContext)
        {
            try
            {
                _logger?.Debug($"start handle event {this._consumerId} {eventContext.Message.ToJson()}");

                var message = eventContext.Message;
                var sagaInfo = eventContext.SagaInfo;
                var messageHandlerTypes = _handlerProvider.GetHandlerTypes(message.GetType());

                if (messageHandlerTypes.Count == 0)
                {
                    _logger?.Debug($"event has no handlerTypes, message:{message.ToJson()}");
                    _internalConsumer.CommitOffset(eventContext);
                    return;
                }

                SaveEvent(eventContext);
                //messageHandlerTypes.ForEach(messageHandlerType =>
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                    {
                        scope.RegisterInstance(typeof(IMessageContext), eventContext);
                        var messageStore = scope.Resolve<IMessageStore>();
                        var subscriptionName = string.Format("{0}.{1}", _subscriptionName, messageHandlerType.Type.FullName);
                        if (!messageStore.HasEventHandled(eventContext.MessageID, subscriptionName))
                        {
                            var eventMessageStates = new List<MessageState>();
                            var commandMessageStates = new List<MessageState>();
                            var eventBus = scope.Resolve<IEventBus>();
                            try
                            {
                                var messageHandler = scope.Resolve(messageHandlerType.Type);
                                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                                   new TransactionOptions
                                                                                   {
                                                                                       IsolationLevel = IsolationLevel.ReadCommitted
                                                                                   },
                                                                                   TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    if (messageHandlerType.IsAsync)
                                    {
                                        await ((dynamic)messageHandler).Handle((dynamic)message)
                                                                       .ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await Task.Run(() =>
                                        {
                                            ((dynamic)messageHandler).Handle((dynamic)message);
                                        }).ConfigureAwait(false);
                                    }

                                    //get commands to be sent
                                    eventBus.GetCommands().ForEach(cmd =>
                                       commandMessageStates.Add(new MessageState(_commandBus?.WrapCommand(cmd, sagaInfo: sagaInfo, producer: Producer)))
                                   );
                                    //get events to be published
                                    eventBus.GetEvents().ForEach(msg =>
                                    {
                                        var topic = msg.GetFormatTopic();
                                        eventMessageStates.Add(new MessageState(_MessageQueueClient.WrapMessage(msg, topic: topic,
                                            key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                    });

                                    eventBus.GetToPublishAnywayMessages().ForEach(msg =>
                                    {
                                        var topic = msg.GetFormatTopic();
                                        eventMessageStates.Add(new MessageState(_MessageQueueClient.WrapMessage(msg, topic: topic, key: msg.Key,
                                            sagaInfo: sagaInfo, producer: Producer)));
                                    });

                                    eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                    messageStore.HandleEvent(eventContext,
                                                           subscriptionName,
                                                           commandMessageStates.Select(s => s.MessageContext),
                                                           eventMessageStates.Select(s => s.MessageContext));

                                    transactionScope.Complete();
                                }
                                if (commandMessageStates.Count > 0)
                                {
                                    _commandBus?.SendMessageStates(commandMessageStates);
                                }
                                if (eventMessageStates.Count > 0)
                                {
                                    _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                                }
                            }
                            catch (Exception e)
                            {
                                if (e is DomainException)
                                {
                                    _logger?.Warn(message.ToJson(), e);
                                }
                                else
                                {
                                    //IO error or sytem Crash
                                    _logger?.Error(message.ToJson(), e);
                                }
                                messageStore.Rollback();
                                eventBus.GetToPublishAnywayMessages().ForEach(msg =>
                                {
                                    var topic = msg.GetFormatTopic();
                                    eventMessageStates.Add(new MessageState(_MessageQueueClient.WrapMessage(msg, topic: topic, key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                });

                                eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e, eventMessageStates.Select(s => s.MessageContext).ToArray());
                                if (eventMessageStates.Count > 0)
                                {
                                    _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Error($"Handle event failed event: {eventContext.ToJson()}", e);
            }
            _internalConsumer.CommitOffset(eventContext);
        }


        private List<MessageState> GetSagaReplyMessageStates(SagaInfo sagaInfo, IEventBus eventBus)
        {
            List<MessageState> eventMessageStates = new List<MessageState>();
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                eventBus.GetSagaResults().ForEach(sagaResult =>
                {
                    var topic = sagaInfo.ReplyEndPoint;
                    if (!string.IsNullOrEmpty(topic))
                    {
                        var sagaReply = _MessageQueueClient.WrapMessage(sagaResult,
                                                                        topic: topic,
                                                                        messageId: ObjectId.GenerateNewId().ToString(),
                                                                        sagaInfo: sagaInfo,
                                                                        producer: Producer);
                        eventMessageStates.Add(new MessageState(sagaReply));
                    }
                });
            }
            return eventMessageStates;
        }


        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_topic))
                {
                    _internalConsumer = _MessageQueueClient.StartSubscriptionClient(_topic, _subscriptionName, _consumerId, OnMessagesReceived);
                }
                _messageProcessor.Start();
            }
            catch (Exception e)
            {
                _logger?.Error(e.GetBaseException().Message, e);
            }
        }

        public void Stop()
        {
            _internalConsumer.Stop();
            _messageProcessor.Stop();
        }

        protected void OnMessagesReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                _messageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
        }

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }
    }
}
