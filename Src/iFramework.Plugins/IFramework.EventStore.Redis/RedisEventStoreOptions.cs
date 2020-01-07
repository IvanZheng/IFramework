namespace IFramework.EventStore.Redis
{
    public class RedisEventStoreOptions
    {
        public int DatabaseName { get; set; } = -1;

        public string ConnectionString { get; set; }

        public string LuaScript { get; set; } = "";
    }
}