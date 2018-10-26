using System.Data.Entity.Core.Objects;
using IFramework.IoC;
using IFramework.UnitOfWork;

namespace IFramework.EntityFramework.Repositories
{
    /// <summary>
    ///     Represents the base class for repositories.
    /// </summary>
    /// <typeparam name="TAggregateRoot">The type of the aggregate root.</typeparam>
    public class DomainRepository : IFramework.Repositories.DomainRepository, IMergeOptionChangable
    {
        #region Construct

        /// <summary>
        ///     Initializes a new instance of DomainRepository.
        /// </summary>
        /// <param name="context">The repository context being used by the repository.</param>
        public DomainRepository(object dbContext, IUnitOfWork unitOfWork, IContainer container)
            : base(dbContext, unitOfWork, container) { }

        #endregion

        public void ChangeMergeOption<TEntity>(MergeOption mergeOption) where TEntity : class
        {
            var repository = GetRepository<TEntity>() as IMergeOptionChangable;
            if (repository != null)
            {
                repository.ChangeMergeOption<TEntity>(mergeOption);
            }
        }
    }
}