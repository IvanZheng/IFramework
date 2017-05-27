using System;
using System.Linq.Expressions;

namespace IFramework.Specifications
{
    public class NotSpecification<T> : Specification<T>
        //   where T : class, IEntity
    {
        private readonly ISpecification<T> spec;

        public NotSpecification(ISpecification<T> specification)
        {
            spec = specification;
        }

        public override Expression<Func<T, bool>> GetExpression()
        {
            var body = Expression.Not(spec.GetExpression().Body);
            return Expression.Lambda<Func<T, bool>>(body, spec.GetExpression().Parameters);
        }
    }
}