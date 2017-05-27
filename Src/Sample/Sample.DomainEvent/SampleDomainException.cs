using System.Runtime.Serialization;
using IFramework.Exceptions;
using IFramework.Message;

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