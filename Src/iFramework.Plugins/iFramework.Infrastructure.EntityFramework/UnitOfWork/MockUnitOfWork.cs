using IFramework.Event;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework
{
    public class MockUnitOfWork : BaseUnitOfWork
    {
        public MockUnitOfWork(IDomainEventBus eventBus)
            : base(eventBus)
        {
            _domainEventBus = eventBus;
        }
       

        public override void Commit()
        {
            throw new NotImplementedException();
        }
    }
}
