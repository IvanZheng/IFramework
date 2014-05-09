using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    public class Modify : ConcurrentCommand, ILinearCommand
    {
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
