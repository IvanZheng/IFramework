using IFramework.DependencyInjection;
using Sample.Command.Community;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Applications
{
    public interface ICommunityService
    {
        //[ConcurrentProcess]
        //[Transaction]
        Task<string> ModifyUserEmailAsync(Guid userId, string email);

        [MailboxProcessing("request", "Id")]
       // [ConcurrentProcess]
        Task<(string, int)> MailboxTestAsync(MailboxRequest request);

        object GetMailboxValues();

    }
}
