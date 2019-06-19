using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IFramework.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace IFramework.EntityFrameworkCore
{
    public class ExtensionEntityMaterializerSource : EntityMaterializerSource
    {
        public override Expression CreateMaterializeExpression(IEntityType entityType,
                                                               string entityInstanceName,
                                                               Expression materializationExpression,
                                                               int[] indexMap = null)
        {
            var expression = base.CreateMaterializeExpression(entityType, entityInstanceName, materializationExpression, indexMap);
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType) && expression is BlockExpression blockExpression)
            {
                var property = Expression.Property(blockExpression.Variables[0],
                                                   typeof(Entity).GetProperty("DomainContext",
                                                                              BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException()); //赋值表达式
                var assign = Expression.Assign(property, 
                                               Expression.Property(materializationExpression, typeof(MaterializationContext).GetProperty("Context") ?? throw new InvalidOperationException())); //把基类的实例化表达式变成列表方便插入
                var list = blockExpression.Expressions.ToList(); //因为最后一个表达式是返回实体实例<br>                //所以我们的逻辑代码要放在最后一条语句之前
                list.Insert(list.Count - 1, assign); //重新生成表达式
                expression = Expression.Block(blockExpression.Variables, list);
            }
            return expression;
        }
    }
}