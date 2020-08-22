using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                    TransactionScopeOption scopeOption = TransactionScopeOption.Required);

        Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                         TransactionScopeOption scopeOption = TransactionScopeOption.Required);

        Task CommitAsync(CancellationToken cancellationToken,
                         IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                         TransactionScopeOption scopeOption = TransactionScopeOption.Required);

        void Rollback();
    }
}