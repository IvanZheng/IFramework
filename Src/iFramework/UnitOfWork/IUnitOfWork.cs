

using System;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted,
                    TransactionScopeOption scopOption = TransactionScopeOption.Required);
        Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted,
                         TransactionScopeOption scopOption = TransactionScopeOption.Required);
        void Rollback();
    }
}
