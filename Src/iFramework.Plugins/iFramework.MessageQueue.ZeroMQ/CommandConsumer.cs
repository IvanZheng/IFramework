using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using System;
using System.Data;
using ZeroMQ;
using System.Linq;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using System.Threading;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandConsumer : CommandConsumerBase, IMessageConsumer
    {
        protected string ReceiveEndPoint { get; set; }
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }
        protected Dictionary<string, ZmqSocket> ReplySenders { get; set; }
        protected readonly ILogger _Logger;
        protected Task _ConsumeWorkTask;
        public decimal MessageCount { get; protected set; }
        protected Task _ReceiveWorkTask;
        protected bool _Exit = false;
        protected decimal HandledMessageCount { get; set; }

        public CommandConsumer(IHandlerProvider handlerProvider, string receiveEndPoint)
            : base(handlerProvider)
        {
            MessageQueue = new BlockingCollection<IMessageContext>();
            ReceiveEndPoint = receiveEndPoint;
            ReplySenders = new Dictionary<string, ZmqSocket>();
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
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

        protected virtual void ConsumeMessages()
        {
            while (!_Exit)
            {
                try
                {
                    ConsumeMessage(MessageQueue.Take());
                    HandledMessageCount++;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    _Logger.Error("consuming message error", ex);
                }
            }
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

        protected override void OnMessageHandled(IMessageContext messageContext, IMessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint))
            {
                var replySender = GetReplySender(messageContext.ReplyToEndPoint);
                if (replySender != null)
                {
                    replySender.SendFrame(reply.GetFrame());
                    _Logger.InfoFormat("send reply, commandID:{0}", reply.MessageID);
                }
            }

            if (!string.IsNullOrWhiteSpace(messageContext.FromEndPoint))
            {
                var notificationSender = GetReplySender(messageContext.FromEndPoint);
                if (notificationSender != null)
                {
                    notificationSender.SendFrame(new MessageHandledNotification(messageContext.MessageID)
                                                        .GetFrame());
                    _Logger.InfoFormat("send notification, commandID:{0}", messageContext.MessageID);

                }
            }
        }

        protected virtual void ReceiveMessage(Frame frame)
        {
            var messageContext = frame.GetMessage<MessageContext>();
            if (messageContext != null)
            {
                MessageQueue.Add(messageContext);
            }
        }

        protected override IMessageReply NewReply(string messageId, object result)
        {
            return new MessageReply(messageId, result);
        }


        public string GetStatus()
        {
            return string.Format("consumer queue length: {0}/{1}<br>", MessageCount, HandledMessageCount);
        }
    }
}
