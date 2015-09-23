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

        public static Frame GetFrame(this IMessageContext message)
        {
            return message.GetFrame((short)MessageCode.Message);
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
