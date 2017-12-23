using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.Command.Impl
{
    public class CommandConsumer : IMessageConsumer
    {
        protected CancellationTokenSource _cancellationTokenSource;
        protected string _commandQueueName;
        protected string _consumerId;
        protected IHandlerProvider _handlerProvider;
        protected ICommitOffsetable _internalConsumer;
        protected ILogger _logger;
        //protected Task _consumeMessageTask;
        protected MessageProcessor _messageProcessor;
        protected IMessagePublisher _messagePublisher;
        protected IMessageQueueClient _messageQueueClient;
        protected ConsumerConfig _consumerConfig;
        private string _producer;

        public CommandConsumer(IMessageQueueClient messageQueueClient,
                               IMessagePublisher messagePublisher,
                               IHandlerProvider handlerProvider,
                               string commandQueueName,
                               string consumerId,
                               ConsumerConfig consumerConfig = null)
        {
            _consumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            _commandQueueName = commandQueueName;
            _handlerProvider = handlerProvider;
            _messagePublisher = messagePublisher;
            _consumerId = consumerId;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageQueueClient = messageQueueClient;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(),
                                                     _consumerConfig.MailboxProcessBatchCount);
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType().Name);
        }

        public string Producer => _producer ?? (_producer = $"{_commandQueueName}.{_consumerId}");

        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_commandQueueName))
                {
                    _internalConsumer = _messageQueueClient.StartQueueClient(_commandQueueName, _consumerId,
                                                                             OnMessageReceived,
                                                                             _consumerConfig);
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

        public string GetStatus()
        {
            return ToString();
        }

        public decimal MessageCount { get; set; }

        protected void OnMessageReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                _messageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
        }

        private List<MessageState> GetSagaReplyMessageStates(SagaInfo sagaInfo, IEventBus eventBus)
        {
            var eventMessageStates = new List<MessageState>();
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                eventBus.GetSagaResults()
                        .ForEach(sagaResult =>
                        {
                            var topic = sagaInfo.ReplyEndPoint;
                            if (!string.IsNullOrEmpty(topic))
                            {
                                var sagaReply = _messageQueueClient.WrapMessage(sagaResult,
                                                                                topic: topic,
                                                                                messageId: ObjectId.GenerateNewId().ToString(),
                                                                                sagaInfo: sagaInfo, producer: Producer);
                                eventMessageStates.Add(new MessageState(sagaReply));
                            }
                        });
            }
            return eventMessageStates;
        }

        protected virtual async Task ConsumeMessage(IMessageContext commandContext)
        {
            try
            {
                var command = commandContext.Message as ICommand;
                var needReply = !string.IsNullOrEmpty(commandContext.ReplyToEndPoint);
                var sagaInfo = commandContext.SagaInfo;
                IMessageContext messageReply = null;
                if (command == null)
                {
                    _internalConsumer.CommitOffset(commandContext);
                    return;
                }
                var needRetry = command.NeedRetry;

                using (var scope = IoCFactory.Instance
                                             .ObjectProvider
                                             .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), commandContext)))
                {
                    var messageStore = scope.GetService<IMessageStore>();
                    var eventMessageStates = new List<MessageState>();
                    var commandHandledInfo = messageStore.GetCommandHandledInfo(commandContext.MessageID);
                    if (commandHandledInfo != null)
                    {
                        if (needReply)
                        {
                            messageReply = _messageQueueClient.WrapMessage(commandHandledInfo.Result,
                                                                           commandContext.MessageID,
                                                                           commandContext.ReplyToEndPoint, producer: Producer,
                                                                           messageId: ObjectId.GenerateNewId().ToString());
                            eventMessageStates.Add(new MessageState(messageReply));
                        }
                    }
                    else
                    {
                        var eventBus = scope.GetService<IEventBus>();
                        var messageHandlerType = _handlerProvider.GetHandlerTypes(command.GetType()).FirstOrDefault();
                        _logger?.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                        if (messageHandlerType == null)
                        {
                            _logger?.Debug($"command has no handlerTypes, message:{command.ToJson()}");
                            if (needReply)
                            {
                                messageReply = _messageQueueClient.WrapMessage(new NoHandlerExists(),
                                                                               commandContext.MessageID,
                                                                               commandContext.ReplyToEndPoint, producer: Producer);
                                eventMessageStates.Add(new MessageState(messageReply));
                            }
                        }
                        else
                        {
                            object messageHandler = null;
                            do
                            {
                                try
                                {
                                    if (messageHandler == null)
                                    {
                                        messageHandler = scope.GetService(messageHandlerType.Type);
                                    }

                                    using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                                       new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                                                                       TransactionScopeAsyncFlowOption.Enabled))
                                    {
                                        if (messageHandlerType.IsAsync)
                                        {
                                            await ((dynamic)messageHandler).Handle((dynamic)command)
                                                                           .ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            await Task.Run(() => { ((dynamic)messageHandler).Handle((dynamic)command); }).ConfigureAwait(false);
                                        }
                                        if (needReply)
                                        {
                                            messageReply = _messageQueueClient.WrapMessage(commandContext.Reply,
                                                                                           commandContext.MessageID, commandContext.ReplyToEndPoint,
                                                                                           producer: Producer);
                                            eventMessageStates.Add(new MessageState(messageReply));
                                        }

                                        eventBus.GetEvents()
                                                .ForEach(@event =>
                                                {
                                                    var topic = @event.GetFormatTopic();
                                                    var eventContext = _messageQueueClient.WrapMessage(@event,
                                                                                                       commandContext.MessageID, topic, @event.Key,
                                                                                                       sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });

                                        eventBus.GetToPublishAnywayMessages()
                                                .ForEach(@event =>
                                                {
                                                    var topic = @event.GetFormatTopic();
                                                    var eventContext = _messageQueueClient.WrapMessage(@event,
                                                                                                       commandContext.MessageID, topic,
                                                                                                       @event.Key, sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });

                                        eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                        messageStore.SaveCommand(commandContext, commandContext.Reply,
                                                                 eventMessageStates.Select(s => s.MessageContext).ToArray());
                                        transactionScope.Complete();
                                    }
                                    needRetry = false;
                                }
                                catch (Exception e)
                                {
                                    eventMessageStates.Clear();
                                    messageStore.Rollback();

                                    if (e is DBConcurrencyException && needRetry)
                                    {
                                        eventBus.ClearMessages();
                                    }
                                    else
                                    {
                                        if (needReply)
                                        {
                                            messageReply = _messageQueueClient.WrapMessage(e.GetBaseException(),
                                                                                           commandContext.MessageID,
                                                                                           commandContext.ReplyToEndPoint,
                                                                                           producer: Producer,
                                                                                           messageId: ObjectId.GenerateNewId().ToString());
                                            eventMessageStates.Add(new MessageState(messageReply));
                                        }
                                        eventBus.GetToPublishAnywayMessages()
                                                .ForEach(@event =>
                                                {
                                                    var topic = @event.GetFormatTopic();
                                                    var eventContext = _messageQueueClient.WrapMessage(@event,
                                                                                                       commandContext.MessageID,
                                                                                                       topic, @event.Key, sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });
                                        if (e is DomainException)
                                        {
                                            var domainExceptionEvent = ((DomainException)e).DomainExceptionEvent;
                                            if (domainExceptionEvent != null)
                                            {
                                                var topic = domainExceptionEvent.GetFormatTopic();

                                                var exceptionMessage = _messageQueueClient.WrapMessage(domainExceptionEvent,
                                                                                                       commandContext.MessageID,
                                                                                                       topic,
                                                                                                       producer: Producer);
                                                eventMessageStates.Add(new MessageState(exceptionMessage));
                                            }
                                            _logger?.Warn(command.ToJson(), e);
                                        }
                                        else
                                        {
                                            _logger?.Error(command.ToJson(), e);
                                            //if we meet with unknown exception, we interrupt saga
                                            if (sagaInfo != null)
                                            {
                                                eventBus.FinishSaga(e);
                                            }
                                        }
                                        eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));
                                        messageStore.SaveFailedCommand(commandContext, e,
                                                                       eventMessageStates.Select(s => s.MessageContext).ToArray());
                                        needRetry = false;
                                    }
                                }
                            } while (needRetry);
                        }
                    }
                    if (eventMessageStates.Count > 0)
                    {
                        var sendTask = _messagePublisher.SendAsync(eventMessageStates.ToArray());
                        // we don't need to wait the send task complete here.
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Fatal($"consume command failed", ex);
            }
            _internalConsumer.CommitOffset(commandContext);
        }
    }
}