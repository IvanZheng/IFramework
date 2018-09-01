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

        public override Task<T> ProcessAsync<T>(Func<Task<T>> funcAsync,
                                                IObjectProvider objectProvider,
                                                Type targetType,
                                                object invocationTarget,
                                                MethodInfo method,
                                                MethodInfo methodInvocationTarget,
                                                object[] arguments)
        {
            return TransactionExtension.DoInTransactionAsync(funcAsync,
                                                             IsolationLevel,
                                                             Scope);
        }

        public override Task ProcessAsync(Func<Task> funcAsync,
                                          IObjectProvider objectProvider,
                                          Type targetType,
                                          object invocationTarget,
                                          MethodInfo method,
                                          MethodInfo methodInvocationTarget,
                                          object[] arguments)
        {
            return TransactionExtension.DoInTransactionAsync(funcAsync,
                                                             IsolationLevel,
                                                             Scope);
        }

        public override object Process(Func<dynamic> func,
                                       IObjectProvider objectProvider,
                                       Type targetType,
                                       object invocationTarget,
                                       MethodInfo method,
                                       MethodInfo methodInvocationTarget,
                                       object[] arguments)
        {
            return TransactionExtension.DoInTransaction(func,
                                                        IsolationLevel,
                                                        Scope);
        }

        public override void Process(Action func,
                                     IObjectProvider objectProvider,
                                     Type targetType,
                                     object invocationTarget,
                                     MethodInfo method,
                                     MethodInfo methodInvocationTarget,
                                     object[] arguments)
        {
            TransactionExtension.DoInTransaction(func,
                                                 IsolationLevel,
                                                 Scope);
        }
    }
}