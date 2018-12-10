using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.EntityFrameworkCore.Repositories;
using IFramework.Infrastructure;
using IFramework.MessageStores.MongoDb;
using IFramework.Repositories;
using IFramework.Specifications;
using IFramework.UnitOfWork;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Sample.Persistence.Repositories
{
    public class RepositoryBase<TEntity> : Repository<TEntity> where TEntity : class
    {
        public RepositoryBase(SampleModelContext dbContext, IUnitOfWork unitOfWork, ILoggerFactory loggerFactory) : base(dbContext, unitOfWork)
        {
            //loggerFactory.CreateLogger($"Sample.Persistence.Repositories.RepositoryBase<{typeof(TEntity).Name}>").LogDebug($"AssetDbContext hash code {dbContext.GetHashCode()}");

        }

        protected override TEntity DoFind(ISpecification<TEntity> specification)
        {
            return IAsyncCursorSourceExtensions.FirstOrDefault(Container.GetMongoDbConnection()
                                                     .GetCollection<TEntity>()
                                                     .AsQueryable()
                                                     .Where(specification.GetExpression()));
            //return base.DoFind(specification);
        }

        protected override Task<TEntity> DoFindAsync(ISpecification<TEntity> specification)
        {
            return Container.GetMongoDbConnection()
                            .GetCollection<TEntity>()
                            .AsQueryable()
                            .Where(specification.GetExpression())
                            .FirstOrDefaultAsync();
            //return base.DoFindAsync(specification);
        }

        protected override IQueryable<TEntity> DoFindAll(ISpecification<TEntity> specification, params OrderExpression[] orderExpressions)
        {
            return Container.GetMongoDbConnection()
                            .GetCollection<TEntity>()
                            .AsQueryable()
                            .FindAll(specification, orderExpressions);
            //return base.DoFindAll(specification, orderExpressions);
        }

        protected override IQueryable<TEntity> DoFindAll(params OrderExpression[] orderExpressions)
        {
            return Container.GetMongoDbConnection()
                            .GetCollection<TEntity>()
                            .AsQueryable()
                            .FindAll((ISpecification<TEntity>) null, orderExpressions);
            //return base.DoFindAll(orderExpressions);
        }
    }
}