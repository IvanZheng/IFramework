using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public MessageDuplicatelyHandled() : base("MessageDuplicatelyHandled") { }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
