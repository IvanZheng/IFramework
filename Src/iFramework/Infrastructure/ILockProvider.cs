using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public interface ILockProvider
    {
        Task<ILock> AcquireAsync(string name, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default(CancellationToken), bool continueOnCapturedContext = false);
        Task<bool> IsLockedAsync(string name);
        Task ReleaseAsync(string name);
        Task RenewAsync(string name, TimeSpan? lockExtension = null);
    }
}
