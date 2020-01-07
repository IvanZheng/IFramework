using System;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Event;
using IFramework.Infrastructure;
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
                    local version = 0
                    if redis.call('EXISTS', @commandId) == 1 then
                        return 0
                    end
                    local expectedVersion = redis.call('GET', @aggregateId)
                    if expectedVersion == nil or expectedVersion == @expectedVersion then
                        redis.call('SET', @commandId, @command)    
                        version = redis.call('INCR', @aggregateId)
                       # ZREVRANGEBYSCORE myset +inf -inf WITHSCORES LIMIT 0 1
                    else
                        return 0
                    end

                ";
            }

            _luaScript = LuaScript.Prepare(luaScript);
        }

        public Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            throw new NotImplementedException();
        }

        public async Task AppendEvents(string aggregateId, long expectedVersion, ICommand command, params IEvent[] events)
        {
            var redisResult = await _db.ScriptEvaluateAsync(_luaScript,
                                                            new {
                                                                aggregateId = (RedisKey) aggregateId,
                                                                commandId = (RedisKey) command.Id,
                                                                command = command.ToJson(),
                                                                expectedVersion,
                                                                events = events.ToJson()
                                                            })
                                       .ConfigureAwait(false);
        }
    }
}