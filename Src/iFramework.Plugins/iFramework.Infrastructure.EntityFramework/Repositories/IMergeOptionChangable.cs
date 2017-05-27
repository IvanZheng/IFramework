using System.Data.Entity.Core.Objects;

namespace IFramework.EntityFramework.Repositories
{
    public interface IMergeOptionChangable
    {
        void ChangeMergeOption<TEntity>(MergeOption mergeOption) where TEntity : class;
    }
}