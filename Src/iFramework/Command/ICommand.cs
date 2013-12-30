using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Command
{
    public interface ICommand
    {
    }

    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; set; }
    }

    public interface ILinearCommand : ICommand
    {
        
    }
}
