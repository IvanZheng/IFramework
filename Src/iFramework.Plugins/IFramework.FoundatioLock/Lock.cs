using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFoundatioLock = Foundatio.Lock.ILock;
namespace IFramework.FoundatioLock
{
    public class Lock : ILock
    {
        private readonly IFoundatioLock _lock;

        public Lock(IFoundatioLock @lock)
        {
            _lock = @lock;
        }

        public Task RenewAsync(TimeSpan? lockExtension = null)
        {
            return _lock.RenewAsync(lockExtension);
        }

        public Task ReleaseAsync()
        {
            return _lock.ReleaseAsync();
        }
    }
}
