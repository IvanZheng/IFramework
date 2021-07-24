using System;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;

namespace IFramework.Message.Impl
{
    public static class MessageContextExtension
    {
        public static Lazy<IMessageTypeProvider> MessageTypeProvider = new Lazy<IMessageTypeProvider>(() => 
        ObjectProviderFactory.GetService<IMessageTypeProvider>());

        public static string GetMessageCode(this IMessageContext messageContext, Type type)
        {
            return MessageTypeProvider.Value.GetMessageCode(type) ?? type.GetFullNameWithAssembly();
        }

        public static object GetMessage(this IMessageContext messageContext, object messageBody)
        {
            object message = null;
            if (messageContext == null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }


            if (messageContext.Headers.TryGetValue("MessageType", out var messageType) && messageType != null)
            {
                var type = MessageTypeProvider.Value.GetMessageType(messageType.ToString());
                if (type != null)
                {
                    message = messageBody is string stringBody ? stringBody.ToJsonObject(type) : messageBody.ToJson().ToJsonObject(type);
                }
            }
            return message;
        }
    }
}