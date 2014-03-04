using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IFramework.Message
{
    public interface IHandlerProvider
    {
        object GetHandler(Type messageType);
        IList<object> GetHandlers(Type messageType);
    }
}
