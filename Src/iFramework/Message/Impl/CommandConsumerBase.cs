using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.SysExceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;

namespace IFramework.Message.Impl
{
    public abstract class CommandConsumerBase
    {
        protected IHandlerProvider _handlerProvider;
        protected ILogger _logger;


        public CommandConsumerBase(IHandlerProvider handlerProvider)
        {
            _handlerProvider = handlerProvider;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        protected abstract IMessageReply NewReply(string messageId, object result);
        protected abstract void OnMessageHandled(IMessageContext messageContext, IMessageReply reply);

        protected virtual void ConsumeMessage(IMessageContext commandContext)
        {
            var command = commandContext.Message as ICommand;
            IMessageReply messageReply = null;
            if (command == null)
            {
                return;
            }
            var needRetry = command.NeedRetry;
            PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
            IEnumerable<IMessageContext> eventContexts = null;
            var messageStore = IoCFactory.Resolve<IMessageStore>();
            var eventBus = IoCFactory.Resolve<IEventBus>();
            var commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
            if (commandHasHandled)
            {
                messageReply = NewReply(commandContext.MessageID, new MessageDuplicatelyHandled());
                //new MessageReply(commandContext.MessageID, new MessageDuplicatelyHandled());
            }
            else
            {
                var messageHandler = _handlerProvider.GetHandler(command.GetType());
                _logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                if (messageHandler == null)
                {
                    messageReply = NewReply(commandContext.MessageID, new NoHandlerExists());
                }
                else
                {
                    bool success = false;
                    do
                    {
                        try
                        {
                            using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                               new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
                            {
                                ((dynamic)messageHandler).Handle((dynamic)command);
                                messageReply = NewReply(commandContext.MessageID, commandContext.Reply);
                                eventContexts = messageStore.SaveCommand(commandContext, eventBus.GetMessages());
                                transactionScope.Complete();
                            }
                            needRetry = false;
                            success = true;
                        }
                        catch (Exception e)
                        {
                            if (e is OptimisticConcurrencyException && needRetry)
                            {
                                eventContexts = null;
                                eventBus.ClearMessages();
                            }
                            else
                            {
                                messageReply = NewReply(commandContext.MessageID, e.GetBaseException());
                                if (e is DomainException)
                                {
                                    _logger.Warn(command.ToJson(), e);
                                }
                                else
                                {
                                    _logger.Error(command.ToJson(), e);
                                }
                                messageStore.SaveFailedCommand(commandContext, e);
                                needRetry = false;
                            }
                        }
                    } while (needRetry);
                    if (success && eventContexts != null && eventContexts.Count() > 0)
                    {
                        IoCFactory.Resolve<IEventPublisher>().Publish(eventContexts.ToArray());
                    }
                }
            }
            OnMessageHandled(commandContext, messageReply);
        }
    }
}
