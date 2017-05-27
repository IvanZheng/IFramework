using ZeroMQ;

namespace IFramework.MessageQueue.ZeroMQ
{
    public static class ZeroMessageQueue
    {
        public static ZmqContext ZmqContext { get; } = ZmqContext.Create();
    }
}