using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class NoCommandTopic : Exception
    {
        public NoCommandTopic() : base("Command must have a topic to indicate which queue to send to.")
        {
        }

        protected NoCommandTopic(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}