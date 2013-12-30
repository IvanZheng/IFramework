using IFramework.Message;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.ZeroMQ
{
    public abstract class MessageConsumer : IMessageConsumer
    {
        protected BlockingCollection<IMessageContext> MessageContextQueue { get; set; }
        protected string[] PullEndPoints { get; set; }
        protected ZmqSocket Replier { get; set; }
        protected string ReplyToEndPoint { get; set; }
        protected decimal HandledMessageCount { get; set; }
        public event MessageHandledHandler MessageHandled;

        public decimal MessageCount
        {
            get
            {
                return MessageContextQueue.Count;
            }
        }

        public void PushMessageContext(IMessageContext commandContext)
        {
            MessageContextQueue.Add(commandContext);
        }

        public MessageConsumer(string replyToEndPoint, params string[] pullEndPoints)
        {
            ReplyToEndPoint = replyToEndPoint;
            PullEndPoints = pullEndPoints;
            MessageContextQueue = new BlockingCollection<IMessageContext>();
        }

        protected virtual void OnMessageHandled(MessageReply reply)
        {
            if (this.MessageHandled != null)
            {
                this.MessageHandled(reply);
            }
        }

        void ReceiveMessages(object messageReceiver)
        {
            var receiver = messageReceiver as ZmqSocket;
            if (receiver == null)
            {
                return;
            }
            while (true)
            {
                try
                {
                    var rcvdMsg = receiver.Receive(Encoding.UTF8);
                    var messageContext = rcvdMsg.ToJsonObject<MessageContext>();
                    if (messageContext == null)
                    {
                        continue;
                    }
                    PushMessageContext(messageContext);
                }
                catch (Exception e)
                {
                    Console.Write(e.GetBaseException().Message);
                }
            }
        }

        void ConsumeMessages()
        {
            while (true)
            {
                Consume(MessageContextQueue.Take());
                HandledMessageCount++;
            }
        }

        public virtual void StartConsuming()
        {
            // Pull message
            if (PullEndPoints != null && PullEndPoints.Length > 0)
            {
                PullEndPoints.ForEach(pullEndPoint =>
                {
                    if (!string.IsNullOrWhiteSpace(pullEndPoint))
                    {
                        try
                        {
                            var messageReceiver = CreateConsumerSocket(pullEndPoint);
                            Task.Factory.StartNew(ReceiveMessages, messageReceiver);
                        }
                        catch(Exception e)
                        {
                            Console.Write(e.GetBaseException().Message);
                        }
                    }
                });
            }
            // Consume message
            Task.Factory.StartNew(ConsumeMessages);

            if (!string.IsNullOrWhiteSpace(ReplyToEndPoint))
            {
                Replier = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUSH);
                Replier.Connect(ReplyToEndPoint);
            }
        }

        protected virtual ZmqSocket CreateConsumerSocket(string pullEndPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PULL);
            receiver.Connect(pullEndPoint);
            return receiver;
        }

        public virtual string GetStatus()
        {
            return string.Format("consumer queue length: {0}/{1}<br>", MessageCount, HandledMessageCount);
        }

        protected abstract void Consume(IMessageContext messageContext);
    }
}
