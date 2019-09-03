using System;

namespace IFramework.Command
{
    public interface ISerialCommandManager
    {
        object GetLinearKey(ILinearCommand command);

        void RegisterSerialCommand<TLinearCommand>(Func<TLinearCommand, object> func)
            where TLinearCommand : ILinearCommand;
    }
}