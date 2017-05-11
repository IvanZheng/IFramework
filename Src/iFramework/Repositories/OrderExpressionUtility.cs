using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IFramework.Repositories
{
    public static class OrderExpressionUtility
    {
        public static IQueryable<TEntity> MergeOrderExpression<TEntity>(this IQueryable<TEntity> query, OrderExpression orderExpression, bool hasSorted = false)
        {

            string orderByCMD = string.Empty;
            if (hasSorted)
            {
                if (orderExpression.SortOrder == SortOrder.Descending)
                {
                    orderByCMD = "ThenByDescending";
                }
                else
                {
                    orderByCMD = "ThenBy";
                }
            }
            else
            {
                if (orderExpression.SortOrder == SortOrder.Descending)
                {
                    orderByCMD = "OrderByDescending";
                }
                else
                {
                    orderByCMD = "OrderBy";
                }
            }
            LambdaExpression le = null;
            if (orderExpression is OrderExpression<TEntity>)
            {
                var member = (orderExpression as OrderExpression<TEntity>).OrderByExpression.Body;
                if (member is UnaryExpression)
                {
                    member = (member as UnaryExpression).Operand;
                }
                le = Utility.GetLambdaExpression(typeof(TEntity), member);
            }
            else if (!string.IsNullOrWhiteSpace(orderExpression.OrderByField))
            {
                le = Utility.GetLambdaExpression(typeof(TEntity), orderExpression.OrderByField);
            }
            MethodCallExpression orderByCallExpression =
                    Expression.Call(typeof(Queryable),
                    orderByCMD,
                    new Type[] { typeof(TEntity), le.Body.Type },
                    query.Expression,
                    le);

            return query.Provider.CreateQuery<TEntity>(orderByCallExpression);
        }
    }
}
