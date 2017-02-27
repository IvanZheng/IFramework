using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.SysExceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.Command.Impl
{
    public class CommandConsumer : IMessageConsumer
    {
        protected IHandlerProvider _handlerProvider;
        protected ILogger _logger;
        protected IMessageQueueClient _messageQueueClient;
        protected IMessagePublisher _messagePublisher;
        protected string _commandQueueName;
        protected string _consuemrId;
        protected CancellationTokenSource _cancellationTokenSource;
        //protected Task _consumeMessageTask;
        protected MessageProcessor _messageProcessor;
        protected int _fullLoadThreshold;
        protected int _waitInterval;
        protected Action<IMessageContext> _removeMessageContext;

        public CommandConsumer(IMessageQueueClient messageQueueClient,
                               IMessagePublisher messagePublisher,
                               IHandlerProvider handlerProvider,
                               string commandQueueName,
                               string consumerId,
                               int fullLoadThreshold = 1000,
                               int waitInterval = 1000,
                               int mailboxProcessBatchCount = 100)
        {
            _fullLoadThreshold = fullLoadThreshold;
            _waitInterval = waitInterval;
            _commandQueueName = commandQueueName;
            _handlerProvider = handlerProvider;
            _messagePublisher = messagePublisher;
            _consuemrId = consumerId;
            _cancellationTokenSource = new CancellationTokenSource();
            _messageQueueClient = messageQueueClient;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(), mailboxProcessBatchCount);
            _logger = IoCFactory.IsInit() ? IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType()) : null;
        }

        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_commandQueueName))
                {
                    _removeMessageContext = _messageQueueClient.StartQueueClient(_commandQueueName, _consuemrId, OnMessageReceived, _fullLoadThreshold, _waitInterval);
                }
                _messageProcessor.Start();
            }
            catch (Exception e)
            {
                _logger?.Error(e.GetBaseException().Message, e);
            }
        }

        protected void OnMessageReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                _messageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });

        }

        public void Stop()
        {
            _messageQueueClient.Dispose();
            _messageProcessor.Stop();
        }

        public string GetStatus()
        {
            return this.ToString();
        }

        public decimal MessageCount { get; set; }

        protected async virtual Task ConsumeMessage(IMessageContext commandContext)
        {
            var command = commandContext.Message as ICommand;
            var needReply = !string.IsNullOrEmpty(commandContext.ReplyToEndPoint);
            var sagaInfo = commandContext.SagaInfo;
            IMessageContext messageReply = null;
            if (command == null)
            {
                _removeMessageContext(commandContext);
                return;
            }
            var needRetry = command.NeedRetry;

            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                scope.RegisterInstance(typeof(IMessageContext), commandContext);
                var eventMessageStates = new List<MessageState>();
                var messageStore = scope.Resolve<IMessageStore>();
                var eventBus = scope.Resolve<IEventBus>();
                var commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
                if (commandHasHandled)
                {
                    if (needReply)
                    {
                        messageReply = _messageQueueClient.WrapMessage(new MessageDuplicatelyHandled(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                        eventMessageStates.Add(new MessageState(messageReply));
                    }
                }
                else
                {
                    var messageHandlerType = _handlerProvider.GetHandlerTypes(command.GetType()).FirstOrDefault();
                    _logger?.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                    if (messageHandlerType == null)
                    {
                        if (needReply)
                        {
                            messageReply = _messageQueueClient.WrapMessage(new NoHandlerExists(), commandContext.MessageID, commandContext.ReplyToEndPoint);
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
                                    messageHandler = scope.Resolve(messageHandlerType.Type);
                                }

                                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                   new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
                                                                   TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    if (messageHandlerType.IsAsync)
                                    {

                                        await ((dynamic)messageHandler).Handle((dynamic)command)
                                                                       .ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await Task.Run(() =>
                                        {
                                            ((dynamic)messageHandler).Handle((dynamic)command);
                                        }).ConfigureAwait(false);
                                    }
                                    if (needReply)
                                    {
                                        messageReply = _messageQueueClient.WrapMessage(commandContext.Reply, commandContext.MessageID, commandContext.ReplyToEndPoint);
                                        eventMessageStates.Add(new MessageState(messageReply));
                                    }

                                    eventBus.GetEvents().ForEach(@event =>
                                    {
                                        var topic = @event.GetFormatTopic();
                                        var eventContext = _messageQueueClient.WrapMessage(@event, commandContext.MessageID, topic, @event.Key, sagaInfo: sagaInfo);
                                        eventMessageStates.Add(new MessageState(eventContext));
                                    });

                                    eventBus.GetToPublishAnywayMessages().ForEach(@event =>
                                    {
                                        var topic = @event.GetFormatTopic();
                                        var eventContext = _messageQueueClient.WrapMessage(@event, commandContext.MessageID, topic, @event.Key, sagaInfo: sagaInfo);
                                        eventMessageStates.Add(new MessageState(eventContext));
                                    });

                                    messageStore.SaveCommand(commandContext, eventMessageStates.Select(s => s.MessageContext).ToArray());
                                    transactionScope.Complete();
                                }
                                needRetry = false;
                            }
                            catch (Exception e)
                            {
                                eventMessageStates.Clear();
                                if (e is OptimisticConcurrencyException && needRetry)
                                {
                                    eventBus.ClearMessages();
                                }
                                else
                                {
                                    messageStore.Rollback();
                                    if (needReply)
                                    {
                                        messageReply = _messageQueueClient.WrapMessage(e.GetBaseException(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                                        eventMessageStates.Add(new MessageState(messageReply));
                                    }
                                    eventBus.GetToPublishAnywayMessages().ForEach(@event =>
                                    {
                                        var topic = @event.GetFormatTopic();
                                        var eventContext = _messageQueueClient.WrapMessage(@event, commandContext.MessageID, topic, @event.Key, sagaInfo: sagaInfo);
                                        eventMessageStates.Add(new MessageState(eventContext));
                                    });

                                    if (e is DomainException)
                                    {
                                        _logger?.Warn(command.ToJson(), e);
                                    }
                                    else
                                    {
                                        _logger?.Error(command.ToJson(), e);
                                    }
                                    messageStore.SaveFailedCommand(commandContext, e, eventMessageStates.Select(s => s.MessageContext).ToArray());
                                    needRetry = false;
                                }
                            }
                        } while (needRetry);
                    }
                }
                try
                {
                    if (_messagePublisher != null && eventMessageStates.Count > 0)
                    {
                        _messagePublisher.SendAsync(eventMessageStates.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"_messagePublisher SendAsync error", ex);
                }
                _removeMessageContext(commandContext);
            }
        }
    }
}
