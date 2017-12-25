using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Infrastructure;
using IFramework.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IFramework.EntityFrameworkCore.SqlServer
{
    public class MsDbContext : DbContext, IDbContext
    {
        public MsDbContext(DbContextOptions options)
            : base(options) { }

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

        public virtual void Rollback()
        {
            ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                         .ForEach(e => { e.State = EntityState.Detached; });
            var refreshableObjects = ChangeTracker.Entries()
                                                  .Where(e => e.State == EntityState.Modified || e.State == EntityState.Unchanged)
                                                  .Select(c => c.Entity);
            refreshableObjects.ForEach(Reload);
            ChangeTracker.Entries().ForEach(e => { (e.Entity as AggregateRoot)?.Rollback(); });
        }

        public override int SaveChanges()
        {
            try
            {
                ChangeTracker.Entries()
                             .Where(e => e.State == EntityState.Added)
                             .ForEach(e => { this.InitializeQueryableCollections(e.Entity); });
                return base.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                Rollback();
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                ChangeTracker.Entries()
                             .Where(e => e.State == EntityState.Added)
                             .ForEach(e => { this.InitializeQueryableCollections(e.Entity); });
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                Rollback();
                throw;
            }
        }
    }
}