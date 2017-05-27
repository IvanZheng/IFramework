using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public MessageDuplicatelyHandled() : base("MessageDuplicatelyHandled")
        {
        }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}