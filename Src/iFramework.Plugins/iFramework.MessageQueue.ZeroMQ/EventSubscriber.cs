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
using IFramework.Infrastructure.Logging;
using System.Collections.Concurrent;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class EventSubscriber : EventSubscriberBase, IMessageConsumer
    {
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }
        protected readonly ILogger _Logger;
        protected Task _ConsumeWorkTask;
        protected Task _ReceiveWorkTask;
        protected bool _Exit = false; 
        protected decimal HandledMessageCount { get; set; }
        public decimal MessageCount { get; protected set; }

        string[] SubEndPoints { get; set; }
        List<Task> _ReceiveWorkTasks;
        public EventSubscriber(string subscriptionName, IHandlerProvider handlerProvider, string[] subEndPoints)
            : base(handlerProvider, subscriptionName)
        {
            SubEndPoints = subEndPoints;
            _ReceiveWorkTasks = new List<Task>();
            MessageQueue = new BlockingCollection<IMessageContext>();
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());

        }

        protected virtual void ConsumeMessages()
        {
            try
            {
                while (!_Exit)
                {
                    ConsumeMessage(MessageQueue.Take());
                    HandledMessageCount++;
                }
            }
            catch (Exception ex)
            {
                _Logger.Debug("end consuming message", ex);
            }

        }

        public virtual void Start()
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

        public virtual void Stop()
        {
            _Exit = true;
            if (_ReceiveWorkTask != null)
            {
                var receiveSocket = (_ReceiveWorkTask.AsyncState as ZmqSocket);
                if (_ReceiveWorkTask.Wait(5000))
                {
                    receiveSocket.Close();
                    _ReceiveWorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat("receiver can't be stopped!");
                }
            }
            if (_ConsumeWorkTask != null)
            {
                MessageQueue.CompleteAdding();
                if (_ConsumeWorkTask.Wait(2000))
                {
                    _ConsumeWorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat(" consumer can't be stopped!");
                }
            }
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

        protected virtual ZmqSocket CreateSocket(string subEndPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.SUB);
            receiver.SubscribeAll();
            receiver.Connect(subEndPoint);
            return receiver;
        }


        protected virtual void ReceiveMessages(object arg)
        {
            var messageReceiver = arg as ZmqSocket;
            while (!_Exit)
            {
                try
                {
                    var frame = messageReceiver.ReceiveFrame(TimeSpan.FromSeconds(2));
                    if (frame != null && frame.MessageSize > 0)
                    {
                        ReceiveMessage(frame);
                        MessageCount++;
                    }
                }
                catch (Exception e)
                {
                    _Logger.Debug(e.GetBaseException().Message, e);
                }
            }
        }

        protected virtual void ReceiveMessage(Frame frame)
        {
            var messageContext = System.Text.Encoding
                                            .GetEncoding("utf-8")
                                            .GetString(frame.Buffer)
                                            .ToJsonObject<MessageContext>();

            MessageQueue.Add(messageContext);
        }

        protected override IMessageContext NewMessageContext(IMessage message)
        {
            return new MessageContext(message);
        }


        public virtual string GetStatus()
        {
            return string.Format("consumer queue length: {0}/{1}<br>", MessageCount, HandledMessageCount);
        }
      
    }
}
