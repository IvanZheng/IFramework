using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.MessageStoring
{
    public class Command : Message
    {
        public Command() { }
        public Command(IMessageContext messageContext) :
            base(messageContext)
        {

        }

        public DomainEvent Parent
        {
            get
            {
                return ParentMessage as DomainEvent;
            }
        }

        public IEnumerable<DomainEvent> Children
        {
            get
            {
                return ChildrenMessage.Cast<DomainEvent>();
            }
        }
    }
}
