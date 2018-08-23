using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Applications
{
    public interface ICommunityService
    {
        Task ModifyUserEmailAsync(Guid userId, string email);

    }
}
