using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.SysExceptions;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;
using IFramework.Event;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class EventSubscriber : MessageConsumer<IMessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }
        string[] SubEndPoints { get; set; }
        List<Task> _ReceiveWorkTasks;
        string SubscriptionName { get; set; }
        public EventSubscriber(string subscriptionName, IHandlerProvider handlerProvider, string[] subEndPoints)
        {
            SubscriptionName = subscriptionName;
            HandlerProvider = handlerProvider;
            SubEndPoints = subEndPoints;
            _ReceiveWorkTasks = new List<Task>();
        }

        public override void Start()
        {
            SubEndPoints.ForEach(subEndPoint =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(subEndPoint))
                    {
                        // Receive messages
                        var messageReceiver = CreateSocket(subEndPoint);
                        _ReceiveWorkTasks.Add(Task.Factory.StartNew(ReceiveMessages, messageReceiver, TaskCreationOptions.LongRunning));
                    }
                }
                catch (Exception e)
                {
                    _Logger.Error(e.GetBaseException().Message, e);
                }
            });
            // Consume messages
            _ConsumeWorkTask = Task.Factory.StartNew(ConsumeMessages, TaskCreationOptions.LongRunning);
        }

        public override void Stop()
        {
            base.Stop();
            _ReceiveWorkTasks.ForEach(receiveWorkTask =>
            {
                if (receiveWorkTask.Wait(5000))
                {
                    (receiveWorkTask.AsyncState as ZmqSocket).Close();
                    receiveWorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat("receiver can't be stopped!");
                }
            });
        }

        protected override ZmqSocket CreateSocket(string subEndPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.SUB);
            receiver.SubscribeAll();
            receiver.Connect(subEndPoint);
            return receiver;
        }

        protected override void ReceiveMessage(Frame frame)
        {
            var messageContext = System.Text.Encoding
                                            .GetEncoding("utf-8")
                                            .GetString(frame.Buffer)
                                            .ToJsonObject<MessageContext>();

            MessageQueue.Add(messageContext);
        }

        protected override void ConsumeMessage(IMessageContext messageContext)
        {
            var eventContext = messageContext as MessageContext;
            var message = eventContext.Message;
            var messageHandlerTypes = HandlerProvider.GetHandlerTypes(message.GetType());

            if (messageHandlerTypes.Count == 0)
            {
                return;
            }

            messageHandlerTypes.ForEach(messageHandlerType =>
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = eventContext;
                eventContext.ToBeSentMessageContexts.Clear();
                var messageStore = IoCFactory.Resolve<IMessageStore>();
                var subscriptionName = string.Format("{0}.{1}", SubscriptionName, messageHandlerType.FullName);
                if (!messageStore.HasEventHandled(eventContext.MessageID, subscriptionName))
                {
                    try
                    {
                        var messageHandler = IoCFactory.Resolve(messageHandlerType);
                        ((dynamic)messageHandler).Handle((dynamic)message);
                        var commandContexts = eventContext.ToBeSentMessageContexts;
                        var eventBus = IoCFactory.Resolve<IEventBus>();
                        var messageContexts = new List<MessageContext>();
                        eventBus.GetMessages().ForEach(msg => messageContexts.Add(new MessageContext(msg)));
                        messageStore.SaveEvent(eventContext, subscriptionName, commandContexts, messageContexts);
                        if (commandContexts.Count > 0)
                        {
                            ((CommandBus)IoCFactory.Resolve<ICommandBus>()).SendCommands(commandContexts.AsEnumerable());
                        }
                        if (messageContexts.Count > 0)
                        {
                            IoCFactory.Resolve<IEventPublisher>().Publish(messageContexts.ToArray());
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is DomainException)
                        {
                            _Logger.Warn(message.ToJson(), e);
                        }
                        else
                        {
                            //IO error or sytem Crash
                            _Logger.Error(message.ToJson(), e);
                        }
                        messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e);
                    }
                    finally
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = null;
                    }
                }
            });
        }
    }
}
