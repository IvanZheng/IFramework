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
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure.EventSourcing
{
    public interface IEventSourcingUnitOfWork : IUnitOfWork { }

    public class UnitOfWork : IEventSourcingUnitOfWork
    {
        private readonly IMessageContext _commandContext;
        private readonly InMemoryStore _inMemoryStore;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IEventStore _eventStore;

        protected List<IEventSourcingRepository> Repositories = new List<IEventSourcingRepository>();

        public UnitOfWork(IEventStore eventStore, IEventBus eventBus, IMessageContext commandContext, InMemoryStore inMemoryStore, ILogger<UnitOfWork> logger)
        {
            _eventStore = eventStore;
            _eventBus = eventBus;
            _commandContext = commandContext;
            _inMemoryStore = inMemoryStore;
            _logger = logger;
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
                                        _eventBus.Publish(aggregateEvents);
                                        await _eventStore.AppendEvents(aggregateRoot.Id,
                                                                       e.Version,
                                                                       _commandContext.MessageId,
                                                                       _commandContext.Reply,
                                                                       _eventBus.GetEvents()
                                                                                .ToArray())
                                                         .ConfigureAwait(false);
                                        
                                    }
                                    catch (DbUpdateConcurrencyException)
                                    {
                                        Rollback();
                                        _inMemoryStore.Remove(aggregateRoot.Id);
                                        throw;
                                    }
                                    catch (MessageDuplicatelyHandled ex)
                                    {
                                        _logger.LogWarning(ex);
                                        _eventBus.ClearMessages();
                                        _eventBus.Publish(ex.Events);
                                        _commandContext.Reply = ex.Result;
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