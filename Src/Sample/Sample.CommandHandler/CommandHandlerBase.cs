using IFramework.Event;
using IFramework.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.CommandHandler
{
    public class CommandHandlerBase
    {
        protected IMessagePublisher EventPublisher => IoCFactory.Resolve<IMessagePublisher>();

        protected IDomainRepository DomainRepository => IoCFactory.Resolve<IDomainRepository>();

        public IMessageContext CommandContext => IoCFactory.Resolve<IMessageContext>();
    }
}
