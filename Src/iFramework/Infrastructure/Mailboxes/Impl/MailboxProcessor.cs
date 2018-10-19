using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Mailboxes.Impl
{
    public class MailboxProcessor: IMailboxProcessor
    {
        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Task Process(string key, Func<Task> process)
        {
            throw new NotImplementedException();
        }
    }
}
