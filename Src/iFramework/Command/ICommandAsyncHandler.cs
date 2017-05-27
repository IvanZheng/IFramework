using IFramework.Message;

namespace IFramework.Command
{
    /// <summary>
    ///     This interface defines a command handler interface.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ICommandAsyncHandler<in TCommand> :
        IMessageAsyncHandler<TCommand> where TCommand : class, ICommand
    {
    }
}