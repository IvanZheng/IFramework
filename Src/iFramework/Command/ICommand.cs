using IFramework.Message;

namespace IFramework.Command
{
    public interface ICommand : IMessage
    {
        bool NeedRetry { get; set; }
    }

    public interface ILinearCommand : ICommand
    {
    }
}