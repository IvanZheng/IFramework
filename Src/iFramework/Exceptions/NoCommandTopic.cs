using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Exceptions
{
    public class NoCommandTopic : Exception
    {
        public NoCommandTopic() : base("Command must have a topic to indicate which queue to send to.") { }

        protected NoCommandTopic(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
