using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
        public CommandConsumer(IHandlerProvider handlerProvider,
                               IMessagePublisher messagePublisher,
                               string commandQueueName)
        {
            _commandQueueName = commandQueueName;
            _handlerProvider = handlerProvider;
            _messagePublisher = messagePublisher;
            _messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
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


        protected void OnMessageReceived(IMessageContext messageContext)
        {
            ConsumeMessage(messageContext);
            MessageCount++;
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
            PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
            List<IMessageContext> eventContexts = new List<IMessageContext>();
            var messageStore = IoCFactory.Resolve<IMessageStore>();
            var eventBus = IoCFactory.Resolve<IEventBus>();
            var commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
            if (commandHasHandled)
            {
                messageReply = _messageQueueClient.WrapMessage(new MessageDuplicatelyHandled(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                eventContexts.Add(messageReply);
            }
            else
            {
                var messageHandler = _handlerProvider.GetHandler(command.GetType());
                _logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                if (messageHandler == null)
                {
                    messageReply = _messageQueueClient.WrapMessage(new NoHandlerExists(), commandContext.MessageID, commandContext.ReplyToEndPoint);
                    eventContexts.Add(messageReply);
                }
                else
                {
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
                                eventBus.GetMessages().ForEach(@event => {
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
                                if (e is DomainException)
                                {
                                    _logger.Warn(command.ToJson(), e);
                                }
                                else
                                {
                                    _logger.Error(command.ToJson(), e);
                                }
                                messageStore.SaveFailedCommand(commandContext, e, messageReply);
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
        }
    }
}
