using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Infrastructure;
using IFramework.Repositories;
using IFramework.Specifications;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace IFramework.EntityFrameworkCore.Repositories
{
    public abstract class Repository<TEntity> : BaseRepository<TEntity>
        where TEntity : class
    {
        private DbSet<TEntity> _objectSet;
        protected MsDbContext Container;

        protected Repository(MsDbContext dbContext, IUnitOfWork unitOfWork)
            : base(dbContext, unitOfWork)
        {
            if (dbContext == null)
            {
                throw new Exception("repository could not work without dbContext");
            }

            (unitOfWork as UnitOfWorks.UnitOfWorkBase)?.RegisterDbContext(dbContext);
            Container = dbContext;
        }

        private DbSet<TEntity> DbSet => _objectSet ?? (_objectSet = Container.Set<TEntity>());

        protected override void DoAdd(TEntity entity)
        {
            DbSet.Add(entity);
        }
        protected override Task DoAddAsync(TEntity entity)
        {
            return DbSet.AddAsync(entity);
        }
        protected override bool DoExists(ISpecification<TEntity> specification)
        {
            return DbSet.Any(specification.GetExpression());
        }

        protected override async Task<bool> DoExistsAsync(ISpecification<TEntity> specification)
        {
            return await DbSet.AnyAsync(specification.GetExpression()).ConfigureAwait(false);
        }

        protected override TEntity DoFind(ISpecification<TEntity> specification)
        {
            return DbSet.Where(specification.GetExpression()).FirstOrDefault();
        }

        protected override Task<TEntity> DoFindAsync(ISpecification<TEntity> specification)
        {
            return DbSet.Where(specification.GetExpression()).FirstOrDefaultAsync();
        }

        protected override TEntity DoGetByKey(params object[] keyValues)
        {
            return DbSet.Find(keyValues);
        }

        protected override Task<TEntity> DoGetByKeyAsync(params object[] keyValues)
        {
            return DbSet.FindAsync(keyValues);
        }

        protected override void DoRemove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        protected override void DoUpdate(TEntity entity)
        {
            Container.Entry(entity).State = EntityState.Modified;
        }

        protected override IQueryable<TEntity> DoFindAll(ISpecification<TEntity> specification,
                                                         params OrderExpression[] orderExpressions)
        {
            return DbSet.FindAll(specification, orderExpressions);
        }

        protected override (IQueryable<TEntity>, long) DoPageFind(int pageIndex,
                                                                  int pageSize,
                                                                  ISpecification<TEntity> specification,
                                                                  params OrderExpression[] orderExpressions)
        {
            //checking arguments for this query 
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }

            if (orderExpressions == null || orderExpressions.Length == 0)
            {
                throw new ArgumentNullException($"OrderByExpressionCannotBeNull");
            }

            if (specification == null)
            {
                specification = new AllSpecification<TEntity>();
            }

            var query = DoFindAll(specification, orderExpressions);
            return (query.GetPageElements(pageIndex, pageSize), query.Count());
        }

        protected override async Task<(IQueryable<TEntity>, long)> DoPageFindAsync(int pageIndex,
                                                                                   int pageSize,
                                                                                   ISpecification<TEntity> specification,
                                                                                   params OrderExpression[] orderExpressions)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }

            if (orderExpressions == null || orderExpressions.Length == 0)
            {
                throw new ArgumentNullException($"OrderByExpressionCannotBeNull");
            }

            if (specification == null)
            {
                specification = new AllSpecification<TEntity>();
            }

            var query = DoFindAll(specification, orderExpressions);
            return (query.GetPageElements(pageIndex, pageSize), await query.CountAsync());
        }

        protected override Task DoAddAsync(IEnumerable<TEntity> entities)
        {
            return DbSet.AddRangeAsync(entities);
        }

        protected override void DoAdd(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                DoAdd(entity);
            }
        }

        protected override long DoCount()
        {
            return DbSet.LongCount();
        }

        protected override Task<long> DoCountAsync()
        {
            return DbSet.LongCountAsync();
        }

        protected override long DoCount(ISpecification<TEntity> specification)
        {
            return DbSet.LongCount(specification.GetExpression());
        }

        protected override Task<long> DoCountAsync(ISpecification<TEntity> specification)
        {
            return DbSet.LongCountAsync(specification.GetExpression());
        }

        protected override long DoCount(Expression<Func<TEntity, bool>> specification)
        {
            return DbSet.LongCount(specification);
        }

        protected override Task<long> DoCountAsync(Expression<Func<TEntity, bool>> specification)
        {
            return DbSet.LongCountAsync(specification);
        }

        protected override void DoReload(TEntity entity)
        {
            Container.Entry(entity).Reload();
            (entity as AggregateRoot)?.Rollback();
        }

        protected override async Task DoReloadAsync(TEntity entity)
        {
            await Container.Entry(entity)
                           .ReloadAsync()
                           .ConfigureAwait(false);

            (entity as AggregateRoot)?.Rollback();
        }
    }
}