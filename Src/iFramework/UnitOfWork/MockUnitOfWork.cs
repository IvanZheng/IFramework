using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public class MockUnitOfWork : IUnitOfWork
    {
        public void Dispose() { }

        public void Commit() { }

        public void Rollback() { }

        public Task CommitAsync()
        {
            return Task.FromResult<object>(null);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }
    }
}