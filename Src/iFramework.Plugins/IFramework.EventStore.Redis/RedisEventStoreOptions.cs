namespace IFramework.EventStore.Redis
{
    public class RedisEventStoreOptions
    {
        public int DatabaseName { get; set; } = -1;

        public string ConnectionString { get; set; }

        public string AppendEventsLuaScript { get; set; }

        public string GetEventsLuaScript { get; set; }

        public string HandleEventLuaScript { get; set; }
    }
}