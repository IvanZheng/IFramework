using System;
using System.Runtime.Serialization;

namespace IFramework.Exceptions
{
    public class CurrentMessageContextIsNull : Exception
    {
        public CurrentMessageContextIsNull() : base("CurrentMessageContext is null") { }

        protected CurrentMessageContextIsNull(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}