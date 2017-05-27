using IFramework.Message;
using ZeroMQ;

namespace IFramework.MessageQueue.ZeroMQ
{
    public static class ZeroFrameHelper
    {
        public static Frame GetFrame(this object message, short code)
        {
            return new Frame(message.GetMessageBytes(code));
        }

        public static Frame GetFrame(this IMessageContext message)
        {
            return message.GetFrame((short) MessageCode.Message);
        }

        public static short GetMessageCode(this Frame frame)
        {
            return frame.Buffer.GetMessageCode();
        }

        public static string GetMessage(this Frame frame)
        {
            return frame.Buffer.GetFormattedMessage();
        }

        public static T GetMessage<T>(this Frame frame)
        {
            return frame.Buffer.GetFormattedMessage<T>();
        }
    }
}