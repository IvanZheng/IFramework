using IFramework.Message;
using IFramework.MessageQueue.EQueue.MessageFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.SysExceptions;
using System.Collections.Concurrent;
using EQueueClientsProducers = EQueue.Clients.Producers;
using EQueueClientsConsumers = EQueue.Clients.Consumers;
using EQueueProtocols = EQueue.Protocols;
using IFramework.Command;
using System.Data;

namespace IFramework.MessageQueue.EQueue
{
    public class CommandConsumer : MessageConsumer<IFramework.MessageQueue.EQueue.MessageFormat.MessageContext>
    {
        public static List<CommandConsumer> CommandConsumers = new List<CommandConsumer>();
        public static string GetConsumersStatus()
        {
            var status = string.Empty;

            CommandConsumers.ForEach(commandConsumer => status += commandConsumer.GetStatus());
            return status;
        }
        protected IHandlerProvider HandlerProvider { get; set; }
        protected EQueueClientsProducers.Producer Producer { get; set; }

        public CommandConsumer(string name, EQueueClientsConsumers.ConsumerSetting consumerSetting, string groupName,
                               string subscribeTopic, string brokerAddress, int producerBrokerPort,
                               IHandlerProvider handlerProvider)
            : base(name, consumerSetting, groupName, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
            var producerSetting = new EQueueClientsProducers.ProducerSetting();
            producerSetting.BrokerAddress = brokerAddress;
            producerSetting.BrokerPort = producerBrokerPort;
            Producer = new EQueueClientsProducers.Producer(string.Format("{0}-Reply-Producer", name), producerSetting);
            CommandConsumers.Add(this);
        }

        public override void Start()
        {
            base.Start();
            try
            {
                Producer.Start();
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        void OnMessageHandled(IFramework.Message.IMessageContext messageContext, IMessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint))
            {
                var messageBody = reply.GetMessageBytes();
                Producer.SendAsync(new global::EQueue.Protocols.Message(messageContext.ReplyToEndPoint, messageBody), string.Empty)
                        .ContinueWith(task =>
                        {
                            if (task.Result.SendStatus == EQueueClientsProducers.SendStatus.Success)
                            {
                                _Logger.DebugFormat("send reply, commandID:{0}", reply.MessageID);
                            }
                            else
                            {
                                _Logger.ErrorFormat("Send Reply {0}", task.Result.SendStatus.ToString());
                            }
                        });
            }
        }

        protected override void ConsumeMessage(IFramework.MessageQueue.EQueue.MessageFormat.MessageContext messageContext, EQueueProtocols.QueueMessage queueMessage)
        {

            if (messageContext == null || messageContext.Message == null)
            {
                return;
            }
            var message = messageContext.Message as ICommand;
            if (message == null)
            {
                return;
            }
            MessageReply messageReply = null;
            var needRetry = message.NeedRetry;
            bool commandHasHandled = false;
            IMessageStore messageStore = null;
            try
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                messageStore = IoCFactory.Resolve<IMessageStore>();
                commandHasHandled = messageStore.HasCommandHandled(messageContext.MessageID);
                if (!commandHasHandled)
                {
                    var messageHandler = HandlerProvider.GetHandler(message.GetType());
                    _Logger.InfoFormat("Handle command, commandID:{0}", messageContext.MessageID);

                    if (messageHandler == null)
                    {
                        messageReply = new MessageReply(messageContext.ReplyToEndPoint, messageContext.MessageID, new NoHandlerExists());
                    }
                    else
                    {
                        do
                        {
                            try
                            {
                                ((dynamic)messageHandler).Handle((dynamic)message);
                                messageReply = new MessageReply(messageContext.ReplyToEndPoint, messageContext.MessageID, messageContext.Reply);
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
                    messageReply = new MessageReply(messageContext.ReplyToEndPoint, messageContext.MessageID, new MessageDuplicatelyHandled());
                }
            }
            catch (Exception e)
            {
                messageReply = new MessageReply(messageContext.ReplyToEndPoint, messageContext.MessageID, e.GetBaseException());
                if (e is DomainException)
                {
                    _Logger.Warn(message.ToJson(), e);
                }
                else
                {
                    _Logger.Error(message.ToJson(), e);
                }
                if (messageStore != null)
                {
                    messageStore.SaveFailedCommand(messageContext);
                }
            }
            finally
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = null;
            }
            if (!commandHasHandled)
            {
                OnMessageHandled(messageContext, messageReply);
                HandledMessageCount++;
            }
        }
    }
}
