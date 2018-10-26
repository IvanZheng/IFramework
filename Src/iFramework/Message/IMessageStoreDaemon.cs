using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageStoreDaemon: IDisposable
    {
        void Start();
        void Stop();
        void RemoveSentCommand(string commandId);
        void RemovePublishedEvent(string eventId);
    }
}
