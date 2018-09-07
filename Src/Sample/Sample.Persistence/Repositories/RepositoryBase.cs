using IFramework.EntityFrameworkCore.Repositories;
using IFramework.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Sample.Persistence.Repositories
{
    public class RepositoryBase<TEntity> : Repository<TEntity> where TEntity : class
    {
        public RepositoryBase(SampleModelContext dbContext, IUnitOfWork unitOfWork, ILoggerFactory loggerFactory) : base(dbContext, unitOfWork)
        {
            loggerFactory.CreateLogger($"Sample.Persistence.Repositories.RepositoryBase<{typeof(TEntity).Name}>").LogDebug($"AssetDbContext hash code {dbContext.GetHashCode()}");

        }
    }
}