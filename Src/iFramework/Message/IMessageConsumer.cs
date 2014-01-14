using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageConsumer
    {
        void Start();
        string GetStatus();
        void EnqueueMessage(object message);
        decimal MessageCount { get; }
    }
}
