using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Infrastructure;
using IFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore
{
    public class MsDbContext : DbContext, IDbContext
    {
        public MsDbContext(DbContextOptions options)
            : base(new DbContextOptionsBuilder(options).ReplaceService<IEntityMaterializerSource, ExtensionEntityMaterializerSource>()
                                                       .Options)
        {
        }

        public void Reload<TEntity>(TEntity entity, bool includeSubObjects = true)
            where TEntity : class
        {
            var entry = Entry(entity); 
            Reload((EntityEntry) entry, includeSubObjects);
        }

        private void Reload(EntityEntry entityEntry, bool includeSubObjects)
        { 
            entityEntry.Reload();

            if (includeSubObjects)
            {
                foreach (var referenceEntry in entityEntry.Members.OfType<ReferenceEntry>())
                {
                    if (referenceEntry.IsLoaded)
                    {
                         Reload(referenceEntry.TargetEntry, true);
                    }
                }

                foreach (var collectionEntry in entityEntry.Members.OfType<CollectionEntry>())
                {
                    if (collectionEntry.IsLoaded)
                    {
                        foreach (var entity in collectionEntry.CurrentValue)
                        {
                             Reload(entity);
                        }
                    }
                }
            }

            (entityEntry.Entity as AggregateRoot)?.Rollback();
        }

        public async Task ReloadAsync<TEntity>(TEntity entity, bool includeSubObjects = true, CancellationToken cancellationToken = default)
            where TEntity : class
        {
            var entry = Entry(entity);
            await ReloadAsync((EntityEntry) entry, includeSubObjects, cancellationToken).ConfigureAwait(false);
        }

        private async Task ReloadAsync(EntityEntry entityEntry, bool includeSubObjects, CancellationToken cancellationToken)
        {
            if (includeSubObjects)
            {
                foreach (var referenceEntry in entityEntry.Members.OfType<ReferenceEntry>())
                {
                    if (referenceEntry.IsLoaded)
                    {
                        await ReloadAsync(referenceEntry.TargetEntry, 
                                          true, 
                                          cancellationToken).ConfigureAwait(false);
                    }
                }

                foreach (var collectionEntry in entityEntry.Members.OfType<CollectionEntry>().ToArray())
                {
                    if (collectionEntry.IsLoaded)
                    {
                        foreach (var entity in collectionEntry.CurrentValue.OfType<object>().ToArray())
                        {
                            var subEntityEntry = Entry(entity);
                            await ReloadAsync(subEntityEntry,
                                              true,
                                              cancellationToken).ConfigureAwait(false);
                            subEntityEntry.State = EntityState.Detached;
                        }

                        collectionEntry.CurrentValue = null;
                        collectionEntry.IsLoaded = false;
                    }
                }
            }
            await entityEntry.ReloadAsync(cancellationToken)
                             .ConfigureAwait(false);
            (entityEntry.Entity as AggregateRoot)?.Rollback();
        }

        public void RemoveEntity<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entry = Entry(entity);
            if (entry != null)
            {
                entry.State = EntityState.Deleted;
            }
        }

        public void LoadReference<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : class
            where TEntityProperty : class
        {
            Entry(entity).Reference(expression).Load();
        }

        public Task LoadReferenceAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression, CancellationToken cancellationToken = default)
            where TEntity : class
            where TEntityProperty : class
        {
            return Entry(entity).Reference(expression).LoadAsync(cancellationToken);
        }

        public void LoadCollection<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : class
            where TEntityProperty : class
        {
            Entry(entity).Collection(expression).Load();
        }

        public Task LoadCollectionAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression, CancellationToken cancellationToken = default)
            where TEntity : class
            where TEntityProperty : class
        {
            return Entry(entity).Collection(expression).LoadAsync(cancellationToken);
        }

        public virtual void Rollback()
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            var stateManager = (this as IDbContextDependencies).StateManager;
#pragma warning restore EF1001 // Internal EF Core API usage.
            stateManager?.ResetState();

            //do
            //{
            //    ChangeTracker.Entries()
            //                 .ToArray()
            //                 .ForEach(e => { e.State = EntityState.Detached; });
            //} while (ChangeTracker.Entries().Any());
        }

        protected virtual void OnException(Exception ex)
        {
        }

        public override int SaveChanges()
        {
            try
            {
                ChangeTracker.Entries()
                             .Where(e => e.State == EntityState.Added)
                             .ForEach(e => { this.InitializeMaterializer(e.Entity); });
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                OnException(ex);
                if (ex is DbUpdateConcurrencyException)
                {
                    throw new DBConcurrencyException(ex.Message, ex);
                }

                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ChangeTracker.Entries()
                             .Where(e => e.State == EntityState.Added)
                             .ForEach(e => { this.InitializeMaterializer(e.Entity); });
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                OnException(ex);
                if (ex is DbUpdateConcurrencyException)
                {
                    throw new DBConcurrencyException(ex.Message, ex);
                }

                throw;
            }
        }

        public void ExecuteByStrategy(Action action)
        {
            var strategy = Database.CreateExecutionStrategy();

            strategy.Execute(action);
        }

        public Task ExecuteByStrategyAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken)
        {
            var strategy = Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(task, cancellationToken);
        }

        public void ExecuteInTransaction(Action action)
        { 
            ExecuteByStrategy(() =>
            {
                if (Database.CurrentTransaction == null)
                {
                    using (var transaction = Database.BeginTransaction())
                    {
                        action();
                        transaction.Commit();
                    }
                }
                else
                {
                    action();
                }
            });
        }

        public Task ExecuteInTransactionAsync(Func<Task> task, CancellationToken cancellationToken)
        {
            return ExecuteByStrategyAsync(async c =>
            {
                if (Database.CurrentTransaction == null)
                {
                    using (var transaction = await Database.BeginTransactionAsync(c)
                                                           .ConfigureAwait(false))
                    {
                        await task().ConfigureAwait(false);
                        await transaction.CommitAsync(c)
                                         .ConfigureAwait(false);
                    }
                }
                else
                {
                    await task().ConfigureAwait(false);
                }
            },cancellationToken);
        }

        public bool IsEntryModified(EntityEntry entry, bool includeReferences = true, bool includeCollections = true)
        {
            var modified = false;

            if (entry != null)
            {
                if (entry.State == EntityState.Modified || entry.State == EntityState.Unchanged && (includeReferences && entry.References.Any(r => r.IsModified) ||
                                                                                                    includeCollections && entry.Collections.Any(c => c.IsModified)))
                {
                    modified = true;
                }
                else
                {
                    modified = includeReferences && entry.References.Any(e => IsEntryModified(e.TargetEntry)) ||
                               includeCollections && entry.Collections.Any(e => e.IsLoaded && entry.Collections.Any(c => c.CurrentValue != null && c.CurrentValue.OfType<object>().Any(o => IsEntryModified(Entry(o)))));
                }
            }

            return modified;

        }
    }
}