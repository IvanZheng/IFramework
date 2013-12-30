using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IFramework.Repositories
{
    public class OrderExpression
    {
        public string OrderByField { get; set; }
        public SortOrder SortOrder { get; set; }


        public OrderExpression(string orderByField, SortOrder sortOrder = SortOrder.Unspecified)
        {
            OrderByField = orderByField;
            SortOrder = sortOrder;
        }
    }

    public class OrderExpression<TEntity> : OrderExpression
    {
        public Expression<Func<TEntity, dynamic>> OrderByExpression { get; set; }

        public OrderExpression(Expression<Func<TEntity, dynamic>> orderByExpression, SortOrder sortOrder = SortOrder.Unspecified) :
            base(null, sortOrder)
        {
            OrderByExpression = orderByExpression;
        }
    }
}
