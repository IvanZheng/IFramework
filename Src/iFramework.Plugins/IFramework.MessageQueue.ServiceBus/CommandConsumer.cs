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
    public class CommandConsumer : IFramework.Message.Impl.CommandConsumerBase, IMessageConsumer
    {
        volatile bool _exit = false;
        protected string _commandQueueName;
        protected QueueClient _commandQueueClient;
        protected Task _commandConsumerTask;
        protected ServiceBusClient _serviceBusClient;
        public CommandConsumer(IHandlerProvider handlerProvider,
                               string serviceBusConnectionString,
                               string commandQueueName)
            : base(handlerProvider)
        {
            _serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
            _commandQueueName = commandQueueName;
        }


        protected TopicClient GetReplyProducer(string replyTopicName)
        {
            TopicClient replyProducer = null;
            if (!string.IsNullOrWhiteSpace(replyTopicName))
            {
                replyProducer = _serviceBusClient.GetTopicClient(replyTopicName);
            }
            return replyProducer;
        }

        protected override void OnMessageHandled(IMessageContext messageContext, IMessageReply reply)
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
                            replyProducer.Send((reply as MessageReply).BrokeredMessage);
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
                _commandQueueClient = _serviceBusClient.CreateQueueClient(_commandQueueName);
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
                        var commandContext = new MessageContext(brokeredMessage);
                        ConsumeMessage(commandContext);
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

       

        public void Stop()
        {
            _exit = true;
            if (_commandConsumerTask != null)
            {
                _serviceBusClient.CloseTopicClients();
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

        protected override IMessageReply NewReply(string messageId, object result)
        {
            return new MessageReply(messageId, result);
        }
    }
}
