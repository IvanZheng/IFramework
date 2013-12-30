using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IFramework.Command
{
    public interface ILinearCommandManager
    {
        object GetLinearKey(ILinearCommand command);
        void RegisterLinearCommand<TLinearCommand>(Func<TLinearCommand, object> func) where TLinearCommand : ILinearCommand;
    }
}
