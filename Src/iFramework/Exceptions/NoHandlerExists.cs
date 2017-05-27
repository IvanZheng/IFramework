using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class NoHandlerExists : Exception
    {
        public NoHandlerExists() : base("NoHandlerExists")
        {
        }

        protected NoHandlerExists(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}