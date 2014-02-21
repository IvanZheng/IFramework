using IFramework.Event;
using IFramework.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace Sample.CommandHandler
{
    public class CommandHandlerBase
    {
        protected IEventPublisher EventPublisher
        {
            get
            {
                return IoCFactory.Resolve<IEventPublisher>();
            }
        }

        protected IDomainRepository DomainRepository
        {
            get
            {
                return IoCFactory.Resolve<IDomainRepository>();
            }
        }
    }
}
