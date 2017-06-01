using IFramework.Message;

namespace IFramework.Command
{
    /// <summary>
    ///     This interface defines a command handler interface.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ICommandHandler<in TCommand> :
        IMessageHandler<TCommand> where TCommand : class, ICommand { }
}