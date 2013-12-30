using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.SysException
{
    public class NoCommandHandlerExists : Exception
    {
        public NoCommandHandlerExists() : base("NoneCommandHandlerExists") { }
    }
}
