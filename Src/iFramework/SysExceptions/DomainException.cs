using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFramework.SysExceptions
{
    public class DomainException : Exception
    {
        public DomainException() { }
        public DomainException(string message)
            : base(message)
        {

        }
        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
