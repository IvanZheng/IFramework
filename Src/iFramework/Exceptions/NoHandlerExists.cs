using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFramework.Exceptions
{
    public class NoHandlerExists : Exception
    {
        public NoHandlerExists() : base("NoHandlerExists") { }

        protected NoHandlerExists(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
