using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFramework.Exceptions
{
    public class CurrentMessageContextIsNull : Exception
    {
        public CurrentMessageContextIsNull() : base("CurrentMessageContext is null") { }

        protected CurrentMessageContextIsNull(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

    }
}
