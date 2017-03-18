using IFramework.Message;
using IFramework.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Sample.DomainEvents
{
    [Topic("DomainEvent")]
    public class SampleDomainException : DomainException
    {
        public SampleDomainException(object errorCode, string message)
            : base(errorCode, message)
        {

        }
        public SampleDomainException(object errorCode, params string[] args)
            : base(errorCode, args)
        {

        }

        protected SampleDomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
