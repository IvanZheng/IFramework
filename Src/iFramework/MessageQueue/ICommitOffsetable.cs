using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.MessageQueue
{
    public interface ICommitOffsetable
    {
        string Id { get; }
        void CommitOffset(IMessageContext messageContext);

        void Start();
        void Stop();
    }
}
