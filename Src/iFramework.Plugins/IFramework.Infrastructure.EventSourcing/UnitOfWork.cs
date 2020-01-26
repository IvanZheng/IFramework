using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.Message;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace IFramework.Infrastructure.EventSourcing
{
    public interface IEventSourcingUnitOfWork : IUnitOfWork { }

    public class UnitOfWork : IEventSourcingUnitOfWork
    {
        private readonly IMessageContext _commandContext;
        private readonly InMemoryStore _inMemoryStore;
        private readonly IEventBus _eventBus;
        private readonly IEventStore _eventStore;

        protected List<IEventSourcingRepository> Repositories = new List<IEventSourcingRepository>();

        public UnitOfWork(IEventStore eventStore, IEventBus eventBus, IMessageContext commandContext, InMemoryStore inMemoryStore)
        {
            _eventStore = eventStore;
            _eventBus = eventBus;
            _commandContext = commandContext;
            _inMemoryStore = inMemoryStore;
        }

        public void Dispose() { }

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
                await repository.GetEntries()
                                .Where(e => e.EntityState == EntityState.Added || e.EntityState == EntityState.Modified)
                                .ForEachAsync(async e =>
                                {
                                    var aggregateRoot = e.Entity;
                                    try
                                    {
                                        var aggregateEvents = aggregateRoot.GetDomainEvents().Cast<IEvent>().ToArray();
                                        await _eventStore.AppendEvents(aggregateRoot.Id,
                                                                       e.Version,
                                                                       _commandContext.MessageId,
                                                                       _commandContext.Reply,
                                                                       aggregateEvents)
                                                         .ConfigureAwait(false);
                                        _eventBus.Publish(aggregateEvents);
                                    }
                                    catch (DbUpdateConcurrencyException)
                                    {
                                        Rollback();
                                        _inMemoryStore.Remove(aggregateRoot.Id);
                                        throw;
                                    }
                                });
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