using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.SysExceptions
{
    public class NoMessageId : Exception
    {
        public NoMessageId() : base("NoMessageId")
        {

        }
        protected NoMessageId(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
