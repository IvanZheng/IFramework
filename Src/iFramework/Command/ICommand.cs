using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Command
{
    public interface ICommand : IMessage
    {
        bool NeedRetry { get; set; }
    }

    public interface ILinearCommand : ICommand
    {
        
    }
}
