using IFramework.Event;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEvent
{
    public class ApplicationEvent : IApplicationEvent
    {
        public string ID
        {
            get;
            set;
        }

        public ApplicationEvent()
        {
            ID = ObjectId.GenerateNewId().ToString();
        }
    }
}
