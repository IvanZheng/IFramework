using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection
{
    public class TransactionAttribute : InterceptorAttribute
    {
        public TransactionAttribute(TransactionScopeOption scope = TransactionScopeOption.Required,
                                    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            Scope = scope;
            IsolationLevel = isolationLevel;
        }

        public TransactionScopeOption Scope { get; set; }
        public IsolationLevel IsolationLevel { get; set; }

        public override async Task<object> ProcessAsync(Func<Task<object>> funcAsync,
                                                        Type targetType,
                                                        object invocationTarget,
                                                        MethodInfo method,
                                                        MethodInfo methodInvocationTarget)
        {
            object result = null;
            await TransactionExtension.DoInTransactionAsync(async () => { result = await funcAsync().ConfigureAwait(false); },
                                                            IsolationLevel,
                                                            Scope)
                                      .ConfigureAwait(false);
            return result;
        }

        public override object Process(Func<object> func,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget)
        {
            object result = null;
            TransactionExtension.DoInTransaction(() => result = func(),
                                                 IsolationLevel,
                                                 Scope);
            return result;
        }
    }
}