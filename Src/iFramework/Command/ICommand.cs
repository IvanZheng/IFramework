using IFramework.Message;

namespace IFramework.Command
{
    public interface ICommand : IMessage
    {
    }

    public interface ILinearCommand : ICommand { }
}