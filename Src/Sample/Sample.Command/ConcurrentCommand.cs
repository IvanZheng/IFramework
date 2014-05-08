using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    public class ConcurrentCommand : ICommand
    {
        public bool NeedRetry
        {
            get;
            set;
        }

        public ConcurrentCommand()
        {
            NeedRetry = true;
        }
    }
}
