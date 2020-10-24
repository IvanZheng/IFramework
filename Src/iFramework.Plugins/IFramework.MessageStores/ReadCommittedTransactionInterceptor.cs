using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IFramework.MessageStores.Relational
{
    public class ReadCommittedTransactionInterceptor : DbTransactionInterceptor
    {
        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
                                                                                                    TransactionStartingEventData eventData, 
                                                                                                    InterceptionResult<DbTransaction> result, 
                                                                                                    CancellationToken cancellationToken = default)
        {
            await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
            return InterceptionResult<DbTransaction>.SuppressWithResult(await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken));
        }

        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result)
        {
            base.TransactionStarting(connection, eventData, result);
            return InterceptionResult<DbTransaction>.SuppressWithResult(connection.BeginTransaction(IsolationLevel.ReadCommitted));
        }
    }
}
