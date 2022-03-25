using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace IFramework.Test.EntityFramework;

public static class SqlFunctions
{
    public static bool CollectionContains(IEnumerable collection,
                                          string key)
    {
        throw new NotSupportedException();
    }

    public static bool CollectionLike(IEnumerable collection,
                                      string key)
    {
        throw new NotSupportedException();
    }


    public static void AddSqlFunctions(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(typeof(SqlFunctions).GetMethod(nameof(CollectionLike)) ??
                                   throw new InvalidOperationException())
                    .HasTranslation(e =>
                    {
                        var column = e[0];
                        var key = e[1];
                        return new LikeExpression(column, key, null, null);
                    })
                    .HasParameter("collection").Metadata.TypeMapping = new StringTypeMapping("varchar", DbType.String);

        modelBuilder.HasDbFunction(typeof(SqlFunctions).GetMethod(nameof(CollectionContains)) ??
                                   throw new InvalidOperationException())
                    .HasTranslation(args =>
                    {
                        return new SqlBinaryExpression(ExpressionType.GreaterThanOrEqual,
                                                       new SqlFunctionExpression("INSTR",
                                                                                 args.ToArray(),
                                                                                 true,
                                                                                 new[] {true, true},
                                                                                 typeof(int),
                                                                                 null),
                                                       new SqlConstantExpression(Expression.Constant(0), null),
                                                       typeof(bool),
                                                       null);
                    })
                    .HasParameter("collection").Metadata.TypeMapping = new StringTypeMapping("varchar", DbType.String);
    }
}