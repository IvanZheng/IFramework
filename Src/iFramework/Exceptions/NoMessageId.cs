using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class NoMessageId : Exception
    {
        public NoMessageId() : base("NoMessageId") { }

        protected NoMessageId(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}