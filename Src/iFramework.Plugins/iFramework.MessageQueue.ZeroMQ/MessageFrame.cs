using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message;

namespace IFramework.MessageQueue.ZeroMQ
{
    public enum MessageCode
    {
        Message,
        MessageReply,
        MessageHandledNotification
    }

    public static class ZeroFrameHelper
    {
        static short ToShort(byte byte1, byte byte2)
        {
            return (short)((byte2 << 8) + byte1);
        }

        static void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number & 255);
        }


        public static byte[] GetFrameBytes(this object message, short code)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(message.ToJson());
            byte[] wrappedBuf = new byte[buffer.Length + 2];
            byte byte0, byte1;
            FromShort(code, out byte0, out byte1);
            wrappedBuf[0] = byte0;
            wrappedBuf[1] = byte1;
            Buffer.BlockCopy(buffer, 0, wrappedBuf, 2, buffer.Length);
            return wrappedBuf;
        }

        public static Frame GetFrame(this object message, short code)
        {
            return new Frame(message.GetFrameBytes(code));
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
            return ToShort(frame.Buffer[0], frame.Buffer[1]);
        }

        public static string GetMessage(this Frame frame)
        {
            var message = string.Empty;
            if (frame.BufferSize > 2)
            {
                byte[] messageBuf = new byte[frame.BufferSize - 2];
                Buffer.BlockCopy(frame.Buffer, 2, messageBuf, 0, messageBuf.Length);
                message = System.Text.Encoding.UTF8.GetString(messageBuf);
            }
            return message;
        }

        public static T GetMessage<T>(this Frame frame)
        {
            T message = default(T);
            if (frame.BufferSize > 2)
            {
                byte[] messageBuf = new byte[frame.BufferSize - 2];
                Buffer.BlockCopy(frame.Buffer, 2, messageBuf, 0, messageBuf.Length);
                var json = System.Text.Encoding.UTF8.GetString(messageBuf);
                message = json.ToJsonObject<T>();
            }
            return message;
        }
    }
}
