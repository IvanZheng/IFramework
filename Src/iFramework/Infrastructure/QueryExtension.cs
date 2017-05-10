using IFramework.Repositories;
using IFramework.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure
{
    public static class QueryExtension
    {
        public static IQueryable<TEntity> FindAll<TEntity>(this IQueryable<TEntity> query,
                                                           ISpecification<TEntity> specification,
                                                           params OrderExpression[] orderExpressions)
        {
            return query.FindAll(specification.GetExpression(), orderExpressions);
        }

        public static IQueryable<TEntity> FindAll<TEntity>(this IQueryable<TEntity> query,
                                                           Expression<Func<TEntity, bool>> expression,
                                                           params OrderExpression[] orderExpressions)
        {
            query = query.Where(expression);
            bool hasSorted = false;
            orderExpressions.ForEach(orderExpression =>
            {
                query = query.MergeOrderExpression(orderExpression, hasSorted);
                hasSorted = true;
            });
            return query;
        }
    }
}
