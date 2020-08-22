﻿using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;
using Sample.Command;

namespace Sample.Domain.Model.Bank.Accounts
{
    public class TransactionStatement: ValueObject<TransactionStatement>
    {
        public TransactionInfo Transaction { get; protected set; }
        public TransactionType Type { get; protected set; }

        public TransactionStatement(TransactionInfo transaction, TransactionType type)
        {
            Transaction = transaction;
            Type = type;
        }
    }
}
