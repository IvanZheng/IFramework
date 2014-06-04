using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.MessageStoring
{
    public class Command : Message
    {
        public Status Status { get; set; }

        public Command() { }
        public Command(IMessageContext messageContext) :
            base(messageContext)
        {
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
