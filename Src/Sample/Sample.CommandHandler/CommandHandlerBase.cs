using IFramework.Event;
using IFramework.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.IoC;

namespace Sample.CommandHandler
{
    public class CommandHandlerBase
    {
        protected IMessagePublisher EventPublisher
        {
            get
            {
                return IoCFactory.Resolve<IMessagePublisher>();
            }
        }

        protected IDomainRepository DomainRepository
        {
            get
            {
                return IoCFactory.Resolve<IDomainRepository>();
            }
        }

        public IMessageContext CommandContext
        {
            get
            {
                return IoCFactory.Resolve<IMessageContext>();
            }
        }
    }
}
