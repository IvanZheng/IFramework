using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.ApplicationEvent
{
    [Topic("AppEvent")]
    public class ApplicationEvent : IApplicationEvent
    {
        public ApplicationEvent()
        {
            ID = ObjectId.GenerateNewId().ToString();
        }

        public string ID { get; set; }

        public string Key { get; set; }
    }
}