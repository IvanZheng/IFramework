using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue.ServiceBus.MessageFormat;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.MessageQueue.ServiceBus
{
    public class CommandConsumer : MessageProcessor, IMessageConsumer
    {
        protected IHandlerProvider _handlerProvider;
        //protected Dictionary<string, TopicClient> _replyProducers;
        protected string _commandQueueName;
        protected QueueClient _commandQueueClient;
        protected Task _commandConsumerTask;

        public CommandConsumer(IHandlerProvider handlerProvider,
                               string serviceBusConnectionString,
                               string commandQueueName)
            : base(serviceBusConnectionString)
        {
            _handlerProvider = handlerProvider;
            _commandQueueName = commandQueueName;
            //_replyProducers = new Dictionary<string, TopicClient>();
        }


        protected TopicClient GetReplyProducer(string replyTopicName)
        {
            TopicClient replyProducer = null;
            if (!string.IsNullOrWhiteSpace(replyTopicName))
            {
                //if (!_replyProducers.TryGetValue(replyTopicName, out replyProducer))
                //{
                //    try
                //    {
                //        replyProducer = CreateTopicClient(replyTopicName);
                //        _replyProducers[replyTopicName] = replyProducer;
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.Error(ex.GetBaseException().Message, ex);
                //    }
                //}
                replyProducer = GetTopicClient(replyTopicName);
            }
            return replyProducer;
        }

        void OnMessageHandled(IMessageContext messageContext, MessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint) && reply != null)
            {
                var replyProducer = GetReplyProducer(messageContext.ReplyToEndPoint);
                if (replyProducer != null)
                {
                    while (true)
                    {
                        try
                        {
                            replyProducer.Send(reply.BrokeredMessage);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(1000);
                            _logger.Error(ex.GetBaseException().Message, ex);
                        }
                    }
                    _logger.InfoFormat("send reply, commandID:{0}", reply.MessageID);
                }
            }
        }

        public void Start()
        {
            try
            {
                _commandQueueClient = CreateQueueClient(_commandQueueName);
                _commandConsumerTask = Task.Factory.StartNew(ConsumeMessages, TaskCreationOptions.LongRunning);

            }
            catch (Exception ex)
            {
                _logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected virtual void ConsumeMessages()
        {
            while (!_exit)
            {
                try
                {
                    BrokeredMessage brokeredMessage = null;
                    brokeredMessage = _commandQueueClient.Receive();
                    if (brokeredMessage != null)
                    {
                        ConsumeMessage(brokeredMessage);
                        brokeredMessage.Complete();
                        MessageCount++;
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _logger.Debug(ex.GetBaseException().Message, ex);
                }
            }
        }

        void ConsumeMessage(BrokeredMessage brokeredMessage)
        {
            var commandContext = new MessageContext(brokeredMessage);
            var command = commandContext.Message as ICommand;
            MessageReply messageReply = null;
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
                messageReply = new MessageReply(commandContext.MessageID, new MessageDuplicatelyHandled());
            }
            else
            {
                var messageHandler = _handlerProvider.GetHandler(command.GetType());
                _logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                if (messageHandler == null)
                {
                    messageReply = new MessageReply(commandContext.MessageID, new NoHandlerExists());
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
                                messageReply = new MessageReply(commandContext.MessageID, commandContext.Reply);
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
                                messageReply = new MessageReply(commandContext.MessageID, e.GetBaseException());
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

        void ConsumeMessageOld(BrokeredMessage brokeredMessage)
        {
            var commandContext = new MessageContext(brokeredMessage);
            var message = commandContext.Message as ICommand;
            MessageReply messageReply = null;
            if (message == null)
            {
                return;
            }
            var needRetry = message.NeedRetry;
            bool commandHasHandled = false;
            IMessageStore messageStore = null;
            try
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                messageStore = IoCFactory.Resolve<IMessageStore>();
                commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
                if (!commandHasHandled)
                {
                    var messageHandler = _handlerProvider.GetHandler(message.GetType());
                    _logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                    if (messageHandler == null)
                    {
                        messageReply = new MessageReply(commandContext.MessageID, new NoHandlerExists());
                    }
                    else
                    {
                        //var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        do
                        {
                            try
                            {
                                ((dynamic)messageHandler).Handle((dynamic)message);
                                //unitOfWork.Commit();
                                messageReply = new MessageReply(commandContext.MessageID, commandContext.Reply);
                                needRetry = false;
                            }
                            catch (Exception ex)
                            {
                                if (!(ex is OptimisticConcurrencyException) || !needRetry)
                                {
                                    throw;
                                }
                            }
                        } while (needRetry);
                    }
                }
                else
                {
                    messageReply = new MessageReply(commandContext.MessageID, new MessageDuplicatelyHandled());
                }
            }
            catch (Exception e)
            {
                messageReply = new MessageReply(commandContext.MessageID, e.GetBaseException());
                if (e is DomainException)
                {
                    _logger.Warn(message.ToJson(), e);
                }
                else
                {
                    _logger.Error(message.ToJson(), e);
                }
                if (messageStore != null)
                {
                    messageStore.SaveFailedCommand(commandContext);
                }
            }
            finally
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = null;
            }
            if (!commandHasHandled)
            {
                OnMessageHandled(commandContext, messageReply);
            }
        }


        public void Stop()
        {
            _exit = true;
            if (_commandConsumerTask != null)
            {
                CloseTopicClients();
                _commandQueueClient.Close();
                if (!_commandConsumerTask.Wait(1000))
                {
                    _logger.ErrorFormat("receiver can't be stopped!");
                }
            }
        }

        public string GetStatus()
        {
            return this.ToString();
        }

        public decimal MessageCount { get; set; }
    }
}
