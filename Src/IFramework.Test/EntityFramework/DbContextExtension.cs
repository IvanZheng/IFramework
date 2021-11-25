using System;
using System.Collections.Generic;
using System.Linq;
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
        {
            return property.HasConversion(a => a.ToJson(false, false, true, true),
                                          v => v.ToJsonObject<TProperty>(false, false, true),
                                          valueComparer);
        }
    }
}
