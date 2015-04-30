using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;

namespace IFramework.MessageQueue.ZeroMQ
{
    public abstract class MessageConsumer<TMessage> : IInProcMessageConsumer
        where TMessage : class
    {
        protected BlockingCollection<TMessage> MessageQueue { get; set; }
        protected string ReceiveEndPoint { get; set; }
        public decimal MessageCount { get; protected set; }
        protected decimal HandledMessageCount { get; set; }
        protected Dictionary<string, ZmqSocket> ReplySenders { get; set; }
        protected readonly ILogger _Logger;
        protected Task _ConsumeWorkTask;
        protected Task _ReceiveWorkTask;
        protected bool _Exit = false;

        public MessageConsumer()
        {
            MessageQueue = new BlockingCollection<TMessage>();
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        public MessageConsumer(string receiveEndPoint)
            : this()
        {
            ReplySenders = new Dictionary<string, ZmqSocket>();
            ReceiveEndPoint = receiveEndPoint;
        }

        public void EnqueueMessage(object message)
        {
            MessageQueue.Add(message as TMessage);
        }

        protected ZmqSocket GetReplySender(string replyToEndPoint)
        {
            ZmqSocket replySender = null;
            if (!string.IsNullOrWhiteSpace(replyToEndPoint))
            {
                replyToEndPoint = replyToEndPoint.Trim();
                if (!ReplySenders.TryGetValue(replyToEndPoint, out replySender))
                {
                    try
                    {
                        replySender = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUSH);
                        replySender.Connect(replyToEndPoint);
                        ReplySenders[replyToEndPoint] = replySender;
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error(ex.GetBaseException().Message, ex);
                    }
                }
            }
            return replySender;
        }

        protected virtual ZmqSocket CreateSocket(string endPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PULL);
            receiver.Bind(endPoint);
            return receiver;
        }


        public virtual void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ReceiveEndPoint))
                {
                    // Receive messages
                    var messageReceiver = CreateSocket(ReceiveEndPoint);
                    _ReceiveWorkTask = Task.Factory.StartNew(ReceiveMessages, messageReceiver, TaskCreationOptions.LongRunning);
                }
                // Consume messages
                _ConsumeWorkTask = Task.Factory.StartNew(ConsumeMessages, TaskCreationOptions.LongRunning);
            }
            catch (Exception e)
            {
                _Logger.Error(e.GetBaseException().Message, e);
            }

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
            if (ReplySenders != null)
            {
                ReplySenders.Values.ForEach(socket => socket.Close());
            }
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

        protected abstract void ConsumeMessage(TMessage message);

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

        protected abstract void ReceiveMessage(Frame frame);

        public virtual string GetStatus()
        {
            return string.Format("consumer queue length: {0}/{1}<br>", MessageCount, HandledMessageCount);
        }

    }
}
