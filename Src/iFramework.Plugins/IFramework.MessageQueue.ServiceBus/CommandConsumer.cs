using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
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

namespace IFramework.MessageQueue.ServiceBus
{
    public class CommandConsumer : MessageProcessor, IMessageConsumer
    {
        protected IHandlerProvider _handlerProvider;
        protected Dictionary<string, TopicClient> _replyProducers;
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
            _replyProducers = new Dictionary<string, TopicClient>();
        }


        protected TopicClient GetReplyProducer(string replyTopicName)
        {
            TopicClient replyProducer = null;
            if (!string.IsNullOrWhiteSpace(replyTopicName))
            {
                if (!_replyProducers.TryGetValue(replyTopicName, out replyProducer))
                {
                    try
                    {
                        replyProducer = CreateTopicClient(replyTopicName);
                        _replyProducers[replyTopicName] = replyProducer;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.GetBaseException().Message, ex);
                    }
                }
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
                        var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        do
                        {
                            try
                            {
                                ((dynamic)messageHandler).Handle((dynamic)message);
                                unitOfWork.Commit();
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
