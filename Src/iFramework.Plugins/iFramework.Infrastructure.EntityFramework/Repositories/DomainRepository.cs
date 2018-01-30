using System.Data.Entity.Core.Objects;
using IFramework.IoC;
using IFramework.UnitOfWork;

namespace IFramework.EntityFramework.Repositories
{
    /// <summary>
    ///     Represents the base class for repositories.
    /// </summary>
    public class DomainRepository : IFramework.Repositories.DomainRepository, IMergeOptionChangable
    {
        #region Construct

        /// <summary>
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="unitOfWork"></param>
        /// <param name="container"></param>
        public DomainRepository(object dbContext, IUnitOfWork unitOfWork, IContainer container)
            : base(dbContext, unitOfWork, container) { }

        #endregion

        public void ChangeMergeOption<TEntity>(MergeOption mergeOption) where TEntity : class
        {
            if (GetRepository<TEntity>() is IMergeOptionChangable repository)
            {
                repository.ChangeMergeOption<TEntity>(mergeOption);
            }
        }
    }
}