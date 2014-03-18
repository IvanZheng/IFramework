using IFramework.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.Repositories
{
    public interface IMergeOptionChangable
    {
        void ChangeMergeOption<TEntity>(MergeOption mergeOption) where TEntity : class;
    }
}
