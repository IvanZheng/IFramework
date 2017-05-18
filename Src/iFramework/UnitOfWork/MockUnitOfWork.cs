using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.UnitOfWork
{
    public class MockUnitOfWork : IUnitOfWork
    {
        public void Dispose()
        {

        }

        public void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                           TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
           
        }

        public void Rollback()
        {
        }

        public Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                   TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            return Task.FromResult<object>(null);
        }

        public Task CommitAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            return Task.FromResult<object>(null);
        }
    }
}
