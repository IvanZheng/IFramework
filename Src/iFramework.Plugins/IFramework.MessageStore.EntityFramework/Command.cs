using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageStoring
{
    public class Command : Message
    {
        public MessageStatus Status { get; set; }
        public string Result { get; set; }
        public string ResultType { get; set; }
        public Command() { }
        public Command(IMessageContext messageContext, object result = null) :
            base(messageContext)
        {
            if (result != null)
            {
                Result = result.ToJson();
                ResultType = result.GetType().AssemblyQualifiedName;
            }
        }

        public object Reply
        {
            get
            {
                object reply = null;
                try
                {
                    if (!string.IsNullOrEmpty(Result) && !string.IsNullOrEmpty(ResultType))
                    {
                        reply = Result.ToJsonObject(System.Type.GetType(ResultType));
                    }
                }
                catch (System.Exception)
                {

                }
                return reply;
            }
        }

        //public Event Parent
        //{
        //    get
        //    {
        //        return ParentMessage as Event;
        //    }
        //}

        //public IEnumerable<Event> Children
        //{
        //    get
        //    {
        //        return ChildrenMessage.Cast<Event>();
        //    }
        //}
    }
}
