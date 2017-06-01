using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class NoCommandHandlerExists : Exception
    {
        public NoCommandHandlerExists() : base("NoneCommandHandlerExists") { }

        protected NoCommandHandlerExists(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}