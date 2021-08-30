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
            Id = ObjectId.GenerateNewId().ToString();
        }

        public string Id { get; set; }

        public string Key { get; set; }
        public string[] Tags { get; set; }
        public string Topic { get; set; }

    }
}