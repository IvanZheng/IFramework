using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFramework.Exceptions
{
    public class NoCommandHandlerExists : Exception
    {
        public NoCommandHandlerExists() : base("NoneCommandHandlerExists") { }
        protected NoCommandHandlerExists(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
