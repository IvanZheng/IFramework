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
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public Command() { }
        public Command(IMessageContext messageContext, Exception ex = null) :
            base(messageContext)
        {
            if (ex != null)
            {
                Error = ex.GetBaseException().Message;
                StackTrace = ex.StackTrace;
            }
        }

        public Event Parent
        {
            get
            {
                return ParentMessage as Event;
            }
        }

        public IEnumerable<Event> Children
        {
            get
            {
                return ChildrenMessage.Cast<Event>();
            }
        }
    }
}
