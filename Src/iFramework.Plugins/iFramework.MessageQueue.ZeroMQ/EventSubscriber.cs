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
using IFramework.MessageQueue.MessageFormat;
using IFramework.SysExceptions;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class EventSubscriber : MessageConsumer<IMessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }
        string[] SubEndPoints { get; set; }
        List<Task> _ReceiveWorkTasks;

        public EventSubscriber(IHandlerProvider handlerProvider, string[] subEndPoints)
        {
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
            var message = messageContext.Message;

            var messageHandlerTypes = HandlerProvider.GetHandlerTypes(message.GetType());
            messageHandlerTypes.ForEach(messageHandlerType =>
            {
                try
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    var messageHandler = IoCFactory.Resolve(messageHandlerType);
                    ((dynamic)messageHandler).Handle((dynamic)message);
                }
                catch (Exception e)
                {
                    if (e is DomainException)
                    {
                        _Logger.Warn(message.ToJson(), e);
                    }
                    else
                    {
                        _Logger.Error(message.ToJson(), e);
                    }
                }
                finally
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = null;
                }
            });
        }
    }
}
