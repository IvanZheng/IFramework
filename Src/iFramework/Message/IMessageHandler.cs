namespace IFramework.Message
{
    public interface IMessageHandler
    {
        void Handle(object message);
    }

    public interface IMessageHandler<in TMessage>
    {
        void Handle(TMessage message);
    }
}