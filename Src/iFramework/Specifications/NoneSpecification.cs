using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace IFramework.Specifications
{
    public sealed class NoneSpecification<T> : Specification<T>
      //  where T : class, IEntity
    {
        public override Expression<Func<T, bool>> GetExpression()
        {
            return o => false;
        }
    }
}
