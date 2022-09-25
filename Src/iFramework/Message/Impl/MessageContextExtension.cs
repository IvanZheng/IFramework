using System;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;

namespace IFramework.Message.Impl
{
    public static class MessageContextExtension
    {
        public static Lazy<IMessageTypeProvider> MessageTypeProvider = new Lazy<IMessageTypeProvider>(() => 
        ObjectProviderFactory.GetService<IMessageTypeProvider>());

        public static string GetMessageCode(this Type type)
        {
            return MessageTypeProvider.Value.GetMessageCode(type) ?? type.GetFullNameWithAssembly();
        }

        public static Type GetMessageType(this IMessageContext messageContext)
        {
            return MessageTypeProvider.Value.GetMessageType(messageContext.Headers["MessageType"]?.ToString());
        }


        public static object GetMessage(object messageBody,
                                        string typeCode)
        {
            var type = MessageTypeProvider.Value.GetMessageType(typeCode);
            if (type != null)
            {
                return messageBody is string stringBody ? stringBody.ToJsonObject(type) : messageBody.ToJson().ToJsonObject(type);
            }

            return null;
        }

        public static object GetMessage(this IMessageContext messageContext, object messageBody, string typeCode = null)
        {
            object message = null;
            if (messageContext == null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            typeCode = typeCode ?? messageContext.MessageType;
            if (!string.IsNullOrWhiteSpace(typeCode))
            {
                var type = MessageTypeProvider.Value.GetMessageType(typeCode);
                if (type != null)
                {
                    message = messageBody is string stringBody ? stringBody.ToJsonObject(type) : messageBody.ToJson().ToJsonObject(type);
                }
            }
            return message;
        }
    }
}