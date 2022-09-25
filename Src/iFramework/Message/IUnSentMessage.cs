using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using IFramework.Message.Impl;

namespace IFramework.Message
{
    public interface IUnSentMessage
    {
        string Id { get; set; }
        string ReplyToEndPoint { get; set; }
        SagaInfo SagaInfo { get; set; }
        string CorrelationId { get; set; }
        string MessageBody { get; set; }
        DateTimeOffset CreateTime { get; set; }
        string Name { get; set; }
        string Type { get; set; }
        string Topic { get; set; }
        string Ip { get; set; }
        string Producer { get; set; }
        string Key { get; set; }
    }
}
