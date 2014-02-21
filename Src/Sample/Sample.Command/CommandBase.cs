using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    public abstract class CommandBase : ICommand
    {
        public bool NeedRetry { get; set; }
       
        public CommandBase()
        {
            NeedRetry = false;
        }
    }

    public abstract class CommandBase<TResult> : CommandBase, ICommand<TResult>
    {
        public CommandBase() { }
        public TResult Result { get; set; }
    }

    public abstract class LinearCommandBase : CommandBase, ILinearCommand
    {
        public LinearCommandBase() : base() { }
    }

    public abstract class LinearCommandBase<TResult> : CommandBase<TResult>, ILinearCommand
    {
        public LinearCommandBase() : base() { }
    }
}
