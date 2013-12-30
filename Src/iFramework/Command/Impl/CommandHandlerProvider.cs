using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Command.Impl
{
    public class CommandHandlerProvider : Message.Impl.HandlerProvider<ICommandHandler<ICommand>>, ICommandHandlerProvider
    {
        public CommandHandlerProvider(params string[] assemblies)
            : base(assemblies)
        {

        }

    }
}
