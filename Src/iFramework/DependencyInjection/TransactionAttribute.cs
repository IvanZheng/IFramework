using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace IFramework.DependencyInjection
{
    public class TransactionAttribute: Attribute
    {
        public TransactionScopeOption Scope { get; set; }
        public IsolationLevel IsolationLevel { get; set; }

        public TransactionAttribute(TransactionScopeOption scope = TransactionScopeOption.Required, 
                                    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            Scope = scope;
            IsolationLevel = isolationLevel;
        }
    }
}
