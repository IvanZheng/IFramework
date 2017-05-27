using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Command;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.Event.Impl
{
    public class EventSubscriber : IMessageConsumer
    {
        private readonly string _topic;
        protected ICommandBus _commandBus;
        protected string _consumerId;
        protected IHandlerProvider _handlerProvider;
        protected ICommitOffsetable _internalConsumer;
        protected ILogger _logger;
        protected MessageProcessor _messageProcessor;
        protected IMessagePublisher _messagePublisher;
        protected IMessageQueueClient _messageQueueClient;

        private string _producer;
        protected string _subscriptionName;

        public EventSubscriber(IMessageQueueClient messageQueueClient,
            IHandlerProvider handlerProvider,
            ICommandBus commandBus,
            IMessagePublisher messagePublisher,
            string subscriptionName,
            string topic,
            string consumerId,
            int mailboxProcessBatchCount = 100)
        {
            _messageQueueClient = messageQueueClient;
            _handlerProvider = handlerProvider;
            _topic = topic;
            _consumerId = consumerId;
            _subscriptionName = subscriptionName;
            _messagePublisher = messagePublisher;
            _commandBus = commandBus;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(),
                mailboxProcessBatchCount);
            _logger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(GetType().Name) : null;
        }

        public string Producer => _producer ?? (_producer = $"{_subscriptionName}.{_topic}.{_consumerId}");


        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_topic))
                    _internalConsumer =
                        _messageQueueClient.StartSubscriptionClient(_topic, _subscriptionName, _consumerId,
                            OnMessagesReceived);
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

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }

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
                _logger?.Debug($"start handle event {_consumerId} {eventContext.Message.ToJson()}");

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
                    using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                    {
                        scope.RegisterInstance(typeof(IMessageContext), eventContext);
                        var messageStore = scope.Resolve<IMessageStore>();
                        var subscriptionName =
                            string.Format("{0}.{1}", _subscriptionName, messageHandlerType.Type.FullName);
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
                                        await ((dynamic) messageHandler).Handle((dynamic) message)
                                            .ConfigureAwait(false);
                                    else
                                        await Task.Run(() => { ((dynamic) messageHandler).Handle((dynamic) message); })
                                            .ConfigureAwait(false);

                                    //get commands to be sent
                                    eventBus.GetCommands().ForEach(cmd =>
                                        commandMessageStates.Add(new MessageState(_commandBus?.WrapCommand(cmd,
                                            sagaInfo: sagaInfo, producer: Producer)))
                                    );
                                    //get events to be published
                                    eventBus.GetEvents().ForEach(msg =>
                                    {
                                        var topic = msg.GetFormatTopic();
                                        eventMessageStates.Add(new MessageState(_messageQueueClient.WrapMessage(msg,
                                            topic: topic,
                                            key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                    });

                                    eventBus.GetToPublishAnywayMessages().ForEach(msg =>
                                    {
                                        var topic = msg.GetFormatTopic();
                                        eventMessageStates.Add(new MessageState(_messageQueueClient.WrapMessage(msg,
                                            topic: topic, key: msg.Key,
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
                                    _commandBus?.SendMessageStates(commandMessageStates);
                                if (eventMessageStates.Count > 0)
                                    _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                            }
                            catch (Exception e)
                            {
                                messageStore.Rollback();
                                if (e is DomainException)
                                {
                                    var exceptionMessage = _messageQueueClient.WrapMessage(e.GetBaseException(),
                                        eventContext.MessageID,
                                        producer: Producer);
                                    eventMessageStates.Add(new MessageState(exceptionMessage));
                                    _logger?.Warn(message.ToJson(), e);
                                }
                                else
                                {
                                    //IO error or sytem Crash
                                    //if we meet with unknown exception, we interrupt saga
                                    if (sagaInfo != null)
                                        eventBus.FinishSaga(e);
                                    _logger?.Error(message.ToJson(), e);
                                }

                                eventBus.GetToPublishAnywayMessages().ForEach(msg =>
                                {
                                    var topic = msg.GetFormatTopic();
                                    eventMessageStates.Add(new MessageState(_messageQueueClient.WrapMessage(msg,
                                        topic: topic, key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                });

                                eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e,
                                    eventMessageStates.Select(s => s.MessageContext).ToArray());
                                if (eventMessageStates.Count > 0)
                                    _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                _logger?.Fatal($"Handle event failed event: {eventContext.ToJson()}", e);
            }
            _internalConsumer.CommitOffset(eventContext);
        }


        private List<MessageState> GetSagaReplyMessageStates(SagaInfo sagaInfo, IEventBus eventBus)
        {
            var eventMessageStates = new List<MessageState>();
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
                eventBus.GetSagaResults().ForEach(sagaResult =>
                {
                    var topic = sagaInfo.ReplyEndPoint;
                    if (!string.IsNullOrEmpty(topic))
                    {
                        var sagaReply = _messageQueueClient.WrapMessage(sagaResult,
                            topic: topic,
                            messageId: ObjectId.GenerateNewId().ToString(),
                            sagaInfo: sagaInfo,
                            producer: Producer);
                        eventMessageStates.Add(new MessageState(sagaReply));
                    }
                });
            return eventMessageStates;
        }

        protected void OnMessagesReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                _messageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
        }
    }
}