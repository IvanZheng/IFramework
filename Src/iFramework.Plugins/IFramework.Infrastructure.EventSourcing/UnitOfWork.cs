using System;
using System.Collections.Generic;
using System.Data;
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
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.Infrastructure.EventSourcing
{
    public interface IEventSourcingUnitOfWork : IUnitOfWork { }

    public class UnitOfWork : IEventSourcingUnitOfWork
    {
        private readonly IMessageContext _commandContext;
        private readonly IEventBus _eventBus;
        private readonly IEventStore _eventStore;
        private readonly IInMemoryStore _inMemoryStore;
        private readonly ILogger _logger;

        protected List<IEventSourcingRepository> Repositories = new List<IEventSourcingRepository>();

        public UnitOfWork(IEventStore eventStore, IEventBus eventBus, IMessageContext commandContext, IInMemoryStore inMemoryStore, ILogger<UnitOfWork> logger)
        {
            _eventStore = eventStore;
            _eventBus = eventBus;
            _commandContext = commandContext;
            _inMemoryStore = inMemoryStore;
            _logger = logger;
        }

        public void Dispose() { }

        public void Commit()
        {
            CommitAsync().GetAwaiter().GetResult();
        }

        public Task CommitAsync()
        {
            return CommitAsync(CancellationToken.None);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            var aggregateRoots = Repositories.SelectMany(r => r.GetEntries()
                                                               .Where(e => e.EntityState == EntityState.Added || e.EntityState == EntityState.Modified))
                                             .ToArray();
            if (aggregateRoots.Length > 1)
            {
                throw new Exception("EventSourcing only supports to operate one aggregate root per command");
            }

            var aggregateEntry = aggregateRoots.FirstOrDefault();
            var aggregateRoot = aggregateEntry?.Entity;
            var aggregateEvents = aggregateRoot?.GetDomainEvents()
                                               .Cast<IEvent>()
                                               .ToArray();

            var expectedVersion = aggregateEvents == null || aggregateEvents.Length == 0 ? -1 : aggregateEntry.Version;
            try
            {
                await _eventStore.AppendEvents(_commandContext.Key,
                                               expectedVersion,
                                               _commandContext.MessageId,
                                               _commandContext.Reply,
                                               _eventBus.GetSagaResult(),
                                               aggregateEvents,
                                               _eventBus.GetEvents()
                                                        .ToArray())
                                 .ConfigureAwait(false);
                if (expectedVersion >= 0 && aggregateRoot != null)
                {
                    _inMemoryStore.Set(aggregateRoot);
                }
                _eventBus.Publish(aggregateEvents);
            }
            catch (DBConcurrencyException)
            {
                Rollback();
                if (aggregateRoot != null)
                {
                    _inMemoryStore.Remove(aggregateRoot);
                }
                throw;
            }
            catch (AddDuplicatedAggregateRoot ex)
            {
                _logger.LogWarning(ex);
                _eventBus.ClearMessages();
                _commandContext.Reply = ex;
                _eventBus.FinishSaga(ex);
                
            }
            catch (MessageDuplicatelyHandled ex)
            {
                _logger.LogWarning(ex);
                _eventBus.ClearMessages();
                if (ex.AggregateRootEvents?.Length > 0)
                {
                    _eventBus.Publish(ex.AggregateRootEvents);
                }

                if (ex.ApplicationEvents?.Length > 0)
                {
                    _eventBus.Publish(ex.ApplicationEvents);
                }
                _commandContext.Reply = ex.CommandResult;
                if (ex.SagaResult != null)
                {
                    _eventBus.FinishSaga(ex.SagaResult);
                }
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