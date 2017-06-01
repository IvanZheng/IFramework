using System;
using System.Linq;
using IFramework.Message.Impl;

namespace IFramework.Command.Impl
{
    public class CommandHandlerProvider : HandlerProvider, ICommandHandlerProvider
    {
        private Type[] _HandlerGenericTypes;

        public CommandHandlerProvider(params string[] assemblies)
            : base(assemblies) { }

        protected override Type[] HandlerGenericTypes
        {
            get
            {
                return _HandlerGenericTypes ?? (_HandlerGenericTypes = new[]
                                                    {
                                                        typeof(ICommandAsyncHandler<ICommand>),
                                                        typeof(ICommandHandler<ICommand>)
                                                    }
                                                    .Select(ht => ht.GetGenericTypeDefinition())
                                                    .ToArray());
            }
        }
    }
}