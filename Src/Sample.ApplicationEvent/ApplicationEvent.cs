using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEvent
{
    [Topic("AppEventTopic")]
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
