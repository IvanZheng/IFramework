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

namespace IFramework.MessageQueue.ZeroMQ
{
    public abstract class MessageConsumer<TMessage> : IMessageConsumer
        where TMessage : class
    {
        protected BlockingCollection<TMessage> MessageQueue { get; set; }

        protected string ReceiveEndPoint { get; set; }
        public decimal MessageCount { get; protected set; }
        protected decimal HandledMessageCount { get; set; }
        protected Dictionary<string, ZmqSocket> ReplySenders { get; set; }

        public MessageConsumer()
        {
            MessageQueue = new BlockingCollection<TMessage>();
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
                        Console.WriteLine(ex.GetBaseException().Message);
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
                    Task.Factory.StartNew(ReceiveMessages, messageReceiver);
                }
                // Consume messages
                Task.Factory.StartNew(ConsumeMessages);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.GetBaseException().Message);
            }

        }

        protected virtual void ConsumeMessages()
        {
            while (true)
            {
                ConsumeMessage(MessageQueue.Take());
                HandledMessageCount++;
            }
        }

        protected abstract void ConsumeMessage(TMessage message);

        protected virtual void ReceiveMessages(object arg)
        {
            var messageReceiver = arg as ZmqSocket;
            while (true)
            {
                try
                {
                    var frame = messageReceiver.ReceiveFrame();
                    ReceiveMessage(frame);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.GetBaseException().Message);
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
