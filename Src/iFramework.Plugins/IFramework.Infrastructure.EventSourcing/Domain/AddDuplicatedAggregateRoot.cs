using IFramework.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Infrastructure.EventSourcing.Domain
{
    public class AddDuplicatedAggregateRoot: DomainException
    {
        public AddDuplicatedAggregateRoot() { }

        public AddDuplicatedAggregateRoot(string id) 
            : base(Exceptions.ErrorCode.DuplicatedObject,
                   $"Add duplicated aggregate root with id {id}")
        {

        }
    }
}
