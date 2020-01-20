using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.UnitOfWork;

namespace IFramework.Infrastructure.EventSourcing
{
    public class UnitOfWork: IUnitOfWork
    {

        protected List<IEventSourcingRepository> Repositories = new List<IEventSourcingRepository>();


        public void Dispose()
        {
        }

        public void Commit(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            CommitAsync(isolationLevel, scopeOption).GetAwaiter().GetResult();
        }

        public Task CommitAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            return CommitAsync(CancellationToken.None, isolationLevel, scopeOption);
        }

        public async Task CommitAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            foreach (var repository in Repositories)
            {
                
            }
        }

        public void Rollback()
        {
            Repositories.ForEach(r => r.Reset());
        }

        public void RegisterRepositories<TAggregateRoot>(EventSourcingRepository<TAggregateRoot> eventSourcingRepository)
            where TAggregateRoot : class, IEventSourcingAggregateRoot, new()
        {
            Repositories.Add(eventSourcingRepository);
        }
    }
}
