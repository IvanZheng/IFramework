using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    public class LinearCommandManager : IFramework.Command.Impl.LinearCommandManager
    {
        public LinearCommandManager() : base()
        {
            this.RegisterLinearCommand<Login>(cmd => cmd.UserName);
            this.RegisterLinearCommand<Register>(cmd => "register:" + cmd.UserName);
        }
    }
}
