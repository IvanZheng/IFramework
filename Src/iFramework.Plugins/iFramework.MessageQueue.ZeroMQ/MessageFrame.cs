using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message;

namespace IFramework.MessageQueue.ZeroMQ
{
    public static class ZeroFrameHelper
    {
        public static Frame GetFrame(this object message, short code)
        {
            return new Frame(message.GetMessageBytes(code));
        }

        public static Frame GetFrame(this IMessageReply message)
        {
            return message.GetFrame((short)MessageCode.MessageReply);
        }

        public static Frame GetFrame(this IMessageContext message)
        {
            return message.GetFrame((short)MessageCode.Message);
        }

        public static Frame GetFrame(this IMessageHandledNotification message)
        {
            return message.GetFrame((short)MessageCode.MessageHandledNotification);
        }

        public static short GetMessageCode(this Frame frame)
        {
            return frame.Buffer.GetMessageCode();
        }

        public static string GetMessage(this Frame frame)
        {
            return frame.Buffer.GetMessage();
        }

        public static T GetMessage<T>(this Frame frame)
        {
            return frame.Buffer.GetMessage<T>();
        }
    }
}
