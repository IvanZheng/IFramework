using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.SysExceptions
{
    public class NoCommandTopic : Exception
    {
        public NoCommandTopic() : base("Command must have a topic to indicate which queue to send to.") { }
    }
}
