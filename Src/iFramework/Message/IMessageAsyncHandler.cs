using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public interface IMessageAsyncHandler
    {
        Task Handle(object message);
    }
    public interface IMessageAsyncHandler<in TMessage> where TMessage : class
    {
        Task Handle(TMessage message);
    }
}
