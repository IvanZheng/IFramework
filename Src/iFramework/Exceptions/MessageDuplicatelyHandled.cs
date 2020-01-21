using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public object Result { get; private set; }
        public MessageDuplicatelyHandled(object result) : base("MessageDuplicatelyHandled")
        {
            Result = result;
        }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}