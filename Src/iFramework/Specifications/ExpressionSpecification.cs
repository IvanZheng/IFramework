using System;
using System.Linq.Expressions;

namespace IFramework.Specifications
{
    internal sealed class ExpressionSpecification<T> : Specification<T>
        //  where T : class, IEntity
    {
        private readonly Expression<Func<T, bool>> expression;

        public ExpressionSpecification(Expression<Func<T, bool>> expression)
        {
            this.expression = expression;
        }

        public override Expression<Func<T, bool>> GetExpression()
        {
            return expression;
        }
    }
}