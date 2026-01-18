using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.EntityFrameworkCore
{
#if NET8_0_OR_GREATER
    public class ReleaseActionDbContextPool<TContext>(DbContextOptions<TContext> options, Action<TContext> releaseAction = null, IServiceProvider serviceProvider = null)
        : DbContextPool<TContext>(options, serviceProvider)
#else
    public class ReleaseActionDbContextPool<TContext>(DbContextOptions<TContext> options, Action<TContext> releaseAction = null)
     : DbContextPool<TContext>(options)
#endif
        where TContext : DbContext
    {
        public override void Return(IDbContextPoolable context)
        {
            releaseAction?.Invoke(context as TContext);
            base.Return(context);
        }

        public override ValueTask ReturnAsync(IDbContextPoolable context, CancellationToken cancellationToken = new CancellationToken())
        {
            releaseAction?.Invoke(context as TContext);
            return base.ReturnAsync(context, cancellationToken);
        }
    }
}
