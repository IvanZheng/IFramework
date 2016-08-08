using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Command.Impl
{
    public class CommandHandlerProvider : Message.Impl.HandlerProvider, ICommandHandlerProvider
    {
        public CommandHandlerProvider(params string[] assemblies)
            : base(assemblies)
        {

        }

        Type[] _HandlerGenericTypes;

        protected override Type[] HandlerGenericTypes
        {
            get
            {
                return _HandlerGenericTypes ?? (_HandlerGenericTypes = new Type[]{
                                                                               typeof(ICommandAsyncHandler<ICommand>), 
                                                                               typeof(ICommandHandler<ICommand>)  }    
                                                                            .Select(ht => ht.GetGenericTypeDefinition())
                                                                            .ToArray());

            }
        }
    }
}
