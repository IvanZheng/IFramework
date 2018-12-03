using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.MessageQueue
{
    public interface IMessageConsumer : ICommitOffsetable
    {
        string Id { get; }
        string Status { get; }
        void Start();
        void Stop();
    }
}
