using System;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFoundatioLockProvider = Foundatio.Lock.ILockProvider;

namespace IFramework.FoundatioLock
{
    public class LockProvider : ILockProvider
    {
        private readonly IFoundatioLockProvider _lockProvider;

        public LockProvider(IFoundatioLockProvider lockProvider)
        {
            _lockProvider = lockProvider;
        }

        public async Task<ILock> AcquireAsync(string name,
                                              TimeSpan? lockTimeout = null,
                                              CancellationToken cancellationToken = default(CancellationToken),
                                              bool continueOnCapturedContext = false)
        {
            var @lock = await _lockProvider.AcquireAsync(name, lockTimeout, cancellationToken)
                                           .ConfigureAwait(continueOnCapturedContext);
            return new Lock(@lock);
        }

        public Task<bool> IsLockedAsync(string name)
        {
            return _lockProvider.IsLockedAsync(name);
        }

        public Task ReleaseAsync(string name)
        {
            return _lockProvider.ReleaseAsync(name);
        }

        public Task RenewAsync(string name, TimeSpan? lockExtension = null)
        {
            return _lockProvider.RenewAsync(name, lockExtension);
        }
    }
}