using IFramework.Domain;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.Repositories
{
    public class DomainRepository : IFramework.Repositories.DomainRepository, IMergeOptionChangable
    {
        #region Construct
        /// <summary>
        /// Initializes a new instance of <c>Repository&lt;TAggregateRoot&gt;</c> class.
        /// </summary>
        /// <param name="context">The repository context being used by the repository.</param>
        public DomainRepository(IUnitOfWork unitOfWork)
            : base(unitOfWork)
        {

        }
        #endregion



        public void ChangeMergeOption<TEntity>(MergeOption mergeOption) where TEntity : class, IAggregateRoot
        {
            var repository = _unitOfWork.GetRepository<TEntity>() as IMergeOptionChangable;
            if (repository != null)
            {
                repository.ChangeMergeOption<TEntity>(mergeOption);
            }
        }
    }
}
