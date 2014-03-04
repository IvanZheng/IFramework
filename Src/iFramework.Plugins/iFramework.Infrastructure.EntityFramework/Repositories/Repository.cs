using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using IFramework.Specifications;
using System.Linq.Expressions;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using System.Reflection;
using IFramework.Domain;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using IFramework.Repositories;

namespace IFramework.EntityFramework.Repositories
{
    public class Repository<TEntity> : BaseRepository<TEntity>, IMergeOptionChangable
     where TEntity : class
    {
        public Repository(DbContext dbContext)
        {
            if (dbContext == null)
            {
                throw new Exception("repository could not work without dbContext");
            }
            _Container = dbContext;
        }

        protected DbContext _Container;

        DbSet<TEntity> DbSet
        {
            get
            {
                return _objectSet ?? (_objectSet = _Container.Set<TEntity>());
            }
        }
        DbSet<TEntity> _objectSet;



        protected override void DoAdd(TEntity entity)
        {
            DbSet.Add(entity);
        }

        protected override bool DoExists(ISpecification<TEntity> specification)
        {
            return Count(specification.GetExpression()) > 0;
        }

        protected override TEntity DoFind(ISpecification<TEntity> specification)
        {
            return DbSet.Where(specification.GetExpression()).FirstOrDefault();
        }
      

        protected override TEntity DoGetByKey(params object[] keyValues)
        {
            return DbSet.Find(keyValues);
        }

        protected override void DoRemove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        protected override void DoUpdate(TEntity entity)
        {
            throw new NotImplementedException();
        }

        protected override IQueryable<TEntity> DoFindAll(ISpecification<TEntity> specification, params OrderExpression[] orderExpressions)
        {
            IQueryable<TEntity> query = DbSet.Where(specification.GetExpression());
            bool hasSorted = false;
            orderExpressions.ForEach(orderExpression =>
            {
                query = query.MergeOrderExpression(orderExpression, hasSorted);
                hasSorted = true;
            });
            return query;
        }

        protected override IQueryable<TEntity> DoPageFind(int pageIndex, int pageSize, ISpecification<TEntity> specification, params OrderExpression[] orderExpressions)
        {
            //checking arguments for this query 
            if (pageIndex < 0)
                throw new ArgumentException("InvalidPageIndex");

            if (pageSize <= 0)
                throw new ArgumentException("InvalidPageCount");

            if (orderExpressions == null || orderExpressions.Length == 0)
                throw new ArgumentNullException("OrderByExpressionCannotBeNull");

            if (specification == (ISpecification<TEntity>)null)
            {
                specification = new AllSpecification<TEntity>();
            }
            var query = DoFindAll(specification, orderExpressions);
            return query.GetPageElements(pageIndex, pageSize);
        }

        protected override IQueryable<TEntity> DoPageFind(int pageIndex, int pageSize, ISpecification<TEntity> specification, ref long totalCount, params OrderExpression[] orderExpressions)
        {
            var query = DoPageFind(pageIndex, pageSize, specification, orderExpressions);
            totalCount = Count(specification.GetExpression());
            return query;
        }

        protected override void DoAdd(IQueryable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                DoAdd(entity);
            }
        }

        protected override long DoCount(ISpecification<TEntity> specification)
        {
            return DbSet.LongCount(specification.GetExpression());
        }

        protected override long DoCount(Expression<Func<TEntity, bool>> specification)
        {
            return DbSet.LongCount(specification);
        }
        
        public void ChangeMergeOption<TMergeOptionEntity>(MergeOption mergeOption) where TMergeOptionEntity : class, IAggregateRoot
        {
            ObjectContext objectContext = ((IObjectContextAdapter)_Container).ObjectContext;
            ObjectSet<TMergeOptionEntity> set = objectContext.CreateObjectSet<TMergeOptionEntity>();
            set.MergeOption = mergeOption;
        }
    }
}
