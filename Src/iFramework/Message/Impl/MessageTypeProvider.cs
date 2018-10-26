using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.Message.Impl
{
    public class MessageTypeProvider:IMessageTypeProvider
    {
        private static readonly Dictionary<string, Type> CodeTypeMapping = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> TypeCodeMapping = new Dictionary<Type, string>();


        public IMessageTypeProvider Register(string code, Type messageType)
        {
            CodeTypeMapping[code] = messageType;
            TypeCodeMapping[messageType] = code;
            return this;
        }

        public IMessageTypeProvider Register(string code, string messageType)
        {
            var type = Type.GetType(messageType);
            Register(code, type);
            return this;
        }

        public IMessageTypeProvider Register(IDictionary<string, Type> codeMapping)
        {
            foreach (var keyValuePair in codeMapping)
            {
                Register(keyValuePair.Key, keyValuePair.Value);
            }
            return this;
        }

        public IMessageTypeProvider Register(IDictionary<string, string> codeMapping)
        {
            foreach (var keyValuePair in codeMapping)
            {
                Register(keyValuePair.Key, keyValuePair.Value);
            }
            return this;
        }

        public string GetMessageCode(Type messageType)
        {
            return TypeCodeMapping.TryGetValue(messageType);
        }

        public Type GetMessageType(string code)
        {
            return CodeTypeMapping.TryGetValue(code) ?? Type.GetType(code);
        }
    }
}
