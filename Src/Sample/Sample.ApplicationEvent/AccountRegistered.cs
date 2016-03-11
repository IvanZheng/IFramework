using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEvent
{
    //[Topic("AppEventTopic2")]
    public class AccountRegistered : ApplicationEvent
    {
        public Guid AccountID { get; set; }
        public string UserName { get; set; }
    }
}
