using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes
{
    public interface IMailboxProcessor
    {
        void Start();
        void Stop();
        Task Process(string key, Func<Task> process);
        string Status { get; }
    }
}
