using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.Infrastructure
{
    public static class TransactionExtension
    {
        public static async Task DoInTransactionAsync(Func<Task> func,
                                                      IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                                      TransactionScopeOption scopOption = TransactionScopeOption.Required,
                                                      bool continueOnCapturedContext = false)
        {
            using (var scope = new TransactionScope(scopOption,
                                                    new TransactionOptions {IsolationLevel = isolationLevel},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                await func().ConfigureAwait(continueOnCapturedContext);
                scope.Complete();
            }
        }

        public static async Task<object> DoInTransactionAsync(Func<Task<object>> func,
                                                      IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                                      TransactionScopeOption scopOption = TransactionScopeOption.Required,
                                                      bool continueOnCapturedContext = false)
        {
            using (var scope = new TransactionScope(scopOption,
                                                    new TransactionOptions {IsolationLevel = isolationLevel},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await func().ConfigureAwait(continueOnCapturedContext);
                scope.Complete();
                return result;
            }
        }

        public static void DoInTransaction(Action action,
                                           IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                           TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            using (var scope = new TransactionScope(scopOption,
                                                    new TransactionOptions {IsolationLevel = isolationLevel},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                action();
                scope.Complete();
            }
        }

        public static object DoInTransaction(Func<object> action,
                                           IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                           TransactionScopeOption scopOption = TransactionScopeOption.Required)
        {
            using (var scope = new TransactionScope(scopOption,
                                                    new TransactionOptions {IsolationLevel = isolationLevel},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = action();
                scope.Complete();
                return result;
            }
        }
    }
}
