using System;
using System.Collections.Generic;
using IFramework.Message.Impl;

namespace IFramework.Message
{
    public interface IMessageContext
    {
        IDictionary<string, object> Headers { get; }
        string Key { get; }
        string MessageID { get; }
        string CorrelationID { get; set; }
        string ReplyToEndPoint { get; }
        object Reply { get; set; }
        object Message { get; }
        DateTime SentTime { get; }
        string Topic { get; }
        long Offset { get; }
        SagaInfo SagaInfo { get; }
        string IP { get; set; }
        string Producer { get; set; }
    }
}