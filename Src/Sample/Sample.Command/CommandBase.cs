using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    [Topic("commandqueueC")]
    public abstract class CommandBase : ICommand
    {
        public bool NeedRetry { get; set; }
        
        public CommandBase()
        {
            NeedRetry = false;
            ID = ObjectId.GenerateNewId().ToString();
        }

        public string ID
        {
            get;
            set;
        }

        public string Key { get; set; }
    }

    public abstract class LinearCommandBase : CommandBase, ILinearCommand
    {
        public LinearCommandBase() : base() { }
    }
}
