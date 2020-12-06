using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void Commit();

        Task CommitAsync();

        Task CommitAsync(CancellationToken cancellationToken);

        void Rollback();
    }
}