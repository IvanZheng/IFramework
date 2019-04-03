using System;
using System.Threading.Tasks;
using System.Transactions;

namespace IFramework.Infrastructure
{
    public static class TransactionExtension
    {
        public static async Task DoInTransactionAsync(Func<Task> func,
                                                      IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                                      TransactionScopeOption scopeOption = TransactionScopeOption.Required,
                                                      bool ignoreInTransaction = true)
        {
            if (ignoreInTransaction && Transaction.Current != null)
            {
                await func().ConfigureAwait(false);
            }
            else
            {
                using (var scope = new TransactionScope(scopeOption,
                                                        new TransactionOptions {IsolationLevel = isolationLevel},
                                                        TransactionScopeAsyncFlowOption.Enabled))
                {
                    await func().ConfigureAwait(false);
                    scope.Complete();
                }
            }
        }

        public static async Task<T> DoInTransactionAsync<T>(Func<Task<T>> func,
                                                            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                                            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
                                                            bool ignoreInTransaction = true)
        {
            if (ignoreInTransaction && Transaction.Current != null)
            {
                return await func().ConfigureAwait(false);
            }

            using (var scope = new TransactionScope(scopeOption,
                                                    new TransactionOptions {IsolationLevel = isolationLevel},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = await func().ConfigureAwait(false);
                scope.Complete();
                return result;
            }
        }

        public static void DoInTransaction(Action action,
                                           IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                           TransactionScopeOption scopeOption = TransactionScopeOption.Required,
                                           bool ignoreInTransaction = true)
        {
            if (ignoreInTransaction && Transaction.Current != null)
            {
                action();
            }
            else
            {
                using (var scope = new TransactionScope(scopeOption,
                                                        new TransactionOptions {IsolationLevel = isolationLevel},
                                                        TransactionScopeAsyncFlowOption.Enabled))
                {
                    action();
                    scope.Complete();
                }
            }
        }

        public static object DoInTransaction(Func<object> action,
                                             IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
                                             TransactionScopeOption scopeOption = TransactionScopeOption.Required,
                                             bool ignoreInTransaction = true)
        {
            if (ignoreInTransaction && Transaction.Current != null)
            {
                return action();
            }

            using (var scope = new TransactionScope(scopeOption,
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