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
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IFramework.EntityFrameworkCore
{
    public class MsDbContext : DbContext, IDbContext
    {
        public MsDbContext(DbContextOptions options)
            : base(new DbContextOptionsBuilder(options).ReplaceService<IEntityMaterializerSource, ExtensionEntityMaterializerSource>()
                                                       .Options) { }

        public void Reload<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entry = Entry(entity);
            entry.Reload();
            (entity as AggregateRoot)?.Rollback();
        }

        public async Task ReloadAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entry = Entry(entity);
            await entry.ReloadAsync()
                       .ConfigureAwait(false);
            (entity as AggregateRoot)?.Rollback();
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

        public Task LoadReferenceAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, TEntityProperty>> expression)
            where TEntity : class
            where TEntityProperty : class
        {
            return Entry(entity).Reference(expression).LoadAsync();
        }

        public void LoadCollection<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : class
            where TEntityProperty : class
        {
            Entry(entity).Collection(expression).Load();
        }

        public Task LoadCollectionAsync<TEntity, TEntityProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TEntityProperty>>> expression)
            where TEntity : class
            where TEntityProperty : class
        {
            return Entry(entity).Collection(expression).LoadAsync();
        }

        public virtual void Rollback()
        {
            do
            {
                ChangeTracker.Entries()
                             .ToArray()
                             .ForEach(e => { e.State = EntityState.Detached; });
            } while (ChangeTracker.Entries().Any());
        }

        protected virtual void OnException(Exception ex)
        {
            if (ex is DbUpdateConcurrencyException)
            {
                Rollback();
            }
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
    }
}