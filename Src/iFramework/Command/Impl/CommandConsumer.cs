using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Practices.Unity;

namespace IFramework.Command.Impl
{
    public class CommandConsumer : IMessageConsumer
    {
        protected IHandlerProvider _handlerProvider;
        protected ILogger _logger;
        protected IMessageQueueClient _messageQueueClient;
        protected IMessagePublisher _messagePublisher;
        protected string _commandQueueName;
        protected BlockingCollection<IMessageContext> _commandContexts;
        protected CancellationTokenSource _cancellationTokenSource;
        protected Task _consumeMessageTask;
        protected MessageProcessor _messageProcessor;
        public CommandConsumer(IMessagePublisher messagePublisher,
                               string commandQueueName,
                               IHandlerProvider handlerProvider = null)
        {
            _commandQueueName = commandQueueName;
            _handlerProvider = handlerProvider?? IoCFactory.Resolve<ICommandHandlerProvider>();
            _messagePublisher = messagePublisher;
            _cancellationTokenSource = new CancellationTokenSource();
            _commandContexts = new BlockingCollection<IMessageContext>();
            _messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        protected virtual void OnMessageHandled(IMessageContext reply)
        {
            if (_messagePublisher != null && !string.IsNullOrWhiteSpace(reply.Topic) && reply != null)
            {
                _messagePublisher.Send(reply);
            }
        }

        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_commandQueueName))
                {
                    _messageQueueClient.StartQueueClient(_commandQueueName, OnMessageReceived);
                    
                }
                _consumeMessageTask = Task.Factory.StartNew(ConsumeMessages,
                                                                _cancellationTokenSource.Token,
                                                                TaskCreationOptions.LongRunning,
                                                                TaskScheduler.Default);
                _messageProcessor.Start();
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().Message, e);
            }
            //try
            //{
            //    _commandQueueClient = _serviceBusClient.CreateQueueClient(_commandQueueName);
            //    _commandConsumerTask = Task.Factory.StartNew(ConsumeMessages, TaskCreationOptions.LongRunning);

            //}
            //catch (Exception ex)
            //{
            //    _logger.Error(ex.GetBaseException().Message, ex);
            //}
        }

        void ConsumeMessages()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var commandContext = _commandContexts.Take(_cancellationTokenSource.Token);
                    _messageProcessor.Process(commandContext, ConsumeMessage);
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
                    _logger.Error(ex.GetBaseException().Message, ex);
                }
            }
        }

        public void PostMessage(IMessageContext messageContext)
        {
            _commandContexts.Add(messageContext);
        }

        protected void OnMessageReceived(IMessageContext messageContext)
        {
            _commandContexts.Add(messageContext);
            //ConsumeMessage(messageContext);
            //MessageCount++;
        }

        public void Stop()
        {
            _messageQueueClient.StopQueueClients();
        }

        public string GetStatus()
        {
            return this.ToString();
        }

        public decimal MessageCount { get; set; }

        protected virtual void ConsumeMessage(IMessageContext commandContext)
        {
            var command = commandContext.Message as ICommand;
            IMessageContext messageReply = null;
            if (command == null)
            {
                return;
            }
            var needRetry = command.NeedRetry;

            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                scope.RegisterInstance(typeof(IMessageContext), commandContext);
                List<IMessageContext> eventContexts = new List<IMessageContext>();
                var messageStore = scope.Resolve<IMessageStore>();
                var eventBus = scope.Resolve<IEventBus>();
                var commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
                if (commandHasHandled)
                {
                    messageReply = _messageQueueClient.WrapMessage(new MessageDuplicatelyHandled(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                    eventContexts.Add(messageReply);
                }
                else
                {
                    var messageHandlerType = _handlerProvider.GetHandlerTypes(command.GetType()).FirstOrDefault();
                    _logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                    if (messageHandlerType == null)
                    {
                        messageReply = _messageQueueClient.WrapMessage(new NoHandlerExists(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                        eventContexts.Add(messageReply);
                    }
                    else
                    {
                        var messageHandler = scope.Resolve(messageHandlerType);
                        do
                        {
                            try
                            {
                                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                   new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
                                {
                                    ((dynamic)messageHandler).Handle((dynamic)command);
                                    messageReply = _messageQueueClient.WrapMessage(commandContext.Reply, commandContext.MessageID, commandContext.ReplyToEndPoint);
                                    eventContexts.Add(messageReply);
                                    eventBus.GetMessages().ForEach(@event =>
                                    {
                                        var eventContext = _messageQueueClient.WrapMessage(@event, commandContext.MessageID);
                                        eventContexts.Add(eventContext);
                                    });

                                    messageStore.SaveCommand(commandContext, eventContexts.ToArray());
                                    transactionScope.Complete();
                                }
                                needRetry = false;
                            }
                            catch (Exception e)
                            {
                                eventContexts.Clear();
                                if (e is OptimisticConcurrencyException && needRetry)
                                {
                                    eventBus.ClearMessages();
                                }
                                else
                                {
                                    messageStore.Rollback();
                                    messageReply = _messageQueueClient.WrapMessage(e.GetBaseException(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                                    eventContexts.Add(messageReply);
                                    eventBus.GetToPublishAnywayMessages().ForEach(@event =>
                                    {
                                        var eventContext = _messageQueueClient.WrapMessage(@event, commandContext.MessageID);
                                        eventContexts.Add(eventContext);
                                    });

                                    if (e is DomainException)
                                    {
                                        _logger.Warn(command.ToJson(), e);
                                    }
                                    else
                                    {
                                        _logger.Error(command.ToJson(), e);
                                    }
                                    messageStore.SaveFailedCommand(commandContext, e, eventContexts.ToArray());
                                    needRetry = false;
                                }
                            }
                        } while (needRetry);
                    }
                }
                if (_messagePublisher != null && eventContexts.Count > 0)
                {
                    _messagePublisher.Send(eventContexts.ToArray());
                }
                _messageQueueClient.CompleteMessage(commandContext);
            }
           // PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
        }
    }
}
