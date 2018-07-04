using IFramework.EntityFrameworkCore.Repositories;
using IFramework.UnitOfWork;

namespace Sample.Persistence.Repositories
{
    public class RepositoryBase<TEntity> : Repository<TEntity> where TEntity : class
    {
        public RepositoryBase(SampleModelContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork) { }
    }
}