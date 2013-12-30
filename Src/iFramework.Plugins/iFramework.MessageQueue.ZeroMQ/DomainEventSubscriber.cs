using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.SysException;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DomainEventSubscriber : MessageConsumer
    {
        IHandlerProvider HandlerProvider { get; set; }

        public DomainEventSubscriber(IHandlerProvider handlerProvider, string[] subEndPoints)
            : base(string.Empty, subEndPoints)
        {
            HandlerProvider = handlerProvider;
        }

        protected override ZmqSocket CreateConsumerSocket(string pullEndPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.SUB);
            receiver.SubscribeAll();
            receiver.Connect(pullEndPoint);
            return receiver;
        }

        protected override void Consume(IMessageContext messageContext)
        {
            var message = messageContext.Message;
            var messageHandlers = HandlerProvider.GetHandlers(message.GetType());
            messageHandlers.ForEach(messageHandler =>
            {
                try
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    messageHandler.Handle(message);
                }
                catch (Exception e)
                {
                    Console.Write(e.GetBaseException().Message);
                }
                finally
                {
                    messageContext.ClearItems();
                }
            });
        }
    }
}
