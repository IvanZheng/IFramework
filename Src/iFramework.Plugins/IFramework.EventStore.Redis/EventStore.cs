using System;
using System.Threading.Tasks;
using IFramework.Event;
using StackExchange.Redis;

namespace IFramework.EventStore.Redis
{
    public class EventStore : IEventStore
    {
        private readonly RedisEventStoreOptions _eventStoreOptions;
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private LuaScript _luaScript;

        public EventStore(RedisEventStoreOptions eventStoreOptions)
        {
            _eventStoreOptions = eventStoreOptions;
        }

        private object AsyncState { get; set; }


        public async Task Connect()
        {
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_eventStoreOptions.ConnectionString)
                                                                .ConfigureAwait(false);
            _db = _connectionMultiplexer.GetDatabase(_eventStoreOptions.DatabaseName, AsyncState);

            var luaScript = _eventStoreOptions.LuaScript;
            if (string.IsNullOrWhiteSpace(luaScript))
            {
                luaScript = @"
                    
                ";
            }

            _luaScript = LuaScript.Prepare(luaScript);
        }

        public Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            throw new NotImplementedException();
        }

        public async Task AppendEvents(string id, long expectedVersion, string correlationId, params IEvent[] events)
        {
            var redisResult = await _db.ScriptEvaluateAsync(_luaScript,
                                                            new {
                                                                aggregateId = (RedisKey) id,
                                                                commandId = (RedisKey) correlationId,
                                                                expectedVersion,
                                                                events
                                                            })
                                       .ConfigureAwait(false);
        }
    }
}