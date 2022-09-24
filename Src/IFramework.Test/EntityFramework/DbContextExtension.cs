using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFramework.Test.EntityFramework
{
    public static  class DbContextExtension
    {

        public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(this PropertyBuilder<TProperty> property, ValueComparer<TProperty> valueComparer = null)
            where TProperty : new()

        {
            Expression<Func<TProperty, TProperty, bool>> e = (c1, c2) => c1.ToJson(false, false, true, false) == c2.ToJson(false, false, true, false);

            return property.HasConversion(a => a.ToJson(false, false, true, true),
                                          v => v.ToJsonObject<TProperty>(false, false, true),
                                          valueComparer ?? new ValueComparer<TProperty>(e,
                                                                                        c => c.GetHashCode(),
                                                                                        c => c.ToJson(false, false, true, false).ToJsonObject<TProperty>(false, false, true)));
        }
    }
}
