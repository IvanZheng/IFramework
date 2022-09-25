using System;
using System.Collections.Generic;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.Message
{
    public interface IMessageContext
    {
        IDictionary<string, object> Headers { get; }
        string Key { get; set; }
        string[] Tags { get; }
        string MessageId { get; }
        string CorrelationId { get; set; }
        string ReplyToEndPoint { get; }
        object Reply { get; set; }
        object Message { get; }
        DateTime SentTime { get; }
        string Topic { get; }
        SagaInfo SagaInfo { get; }
        string Ip { get; set; }
        string Producer { get; set; }
        MessageOffset MessageOffset { get; }
        string MessageType { get; set; }
    }
}