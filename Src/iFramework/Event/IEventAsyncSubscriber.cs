using IFramework.Message;

namespace IFramework.Event
{
    public interface IEventAsyncSubscriber<in TEvent> :
        IMessageAsyncHandler<TEvent> { }
}