using IFramework.Message.Impl;
using System;
using System.Collections.Generic;

namespace IFramework.Message
{
    public interface IHandlerProvider
    {
        object GetHandler(Type messageType);
        IList<HandlerTypeInfo> GetHandlerTypes(Type messageType);
    }
}
