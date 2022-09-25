﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.MessageQueue.InMemory
{
    public class MessageContext : IMessageContext
    {
        public MessageContext(string messageBody,
                              string type,
                              string id):this(MessageContextExtension.GetMessage(messageBody, type), id)
        {

        }

        

        public MessageContext(object message, string id = null)
        {
            SentTime = DateTime.Now;
            Message = message;
            if (!string.IsNullOrEmpty(id))
            {
                MessageId = id;
            }
            else if (message is IMessage)
            {
                MessageId = ((IMessage)message).Id;
            }
            else
            {
                MessageId = ObjectId.GenerateNewId().ToString();
            }
            if (message is IMessage iMessage)
            {
                Topic = iMessage.GetTopic();
                Tags = iMessage.Tags;
            }
            MessageOffset = new MessageOffset();
        }
        public IDictionary<string, object> Headers { get; set; }
        public string Key { get; set; }
        public string[] Tags { get; set; }
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyToEndPoint { get; set; }
        public object Reply { get; set; }
        public object Message { get; set; }
        public DateTime SentTime { get; set; }
        public string Topic { get; set; }
        public long Offset { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string Ip { get; set; }
        public string Producer { get; set; }
        public MessageOffset MessageOffset { get; }
        public string MessageType { get; set; }
    }
}
