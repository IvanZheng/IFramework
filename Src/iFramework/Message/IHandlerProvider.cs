using System;
using System.Collections.Generic;
using IFramework.Message.Impl;

namespace IFramework.Message
{
    public interface IHandlerProvider
    {
        object GetHandler(Type messageType);
        IList<HandlerTypeInfo> GetHandlerTypes(Type messageType);
    }
}