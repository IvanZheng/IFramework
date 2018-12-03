using System;

namespace Sample.ApplicationEvent
{
    //[Topic("AppEventTopic2")]
    public class AccountRegistered : ApplicationEvent
    {
        public Guid AccountID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}