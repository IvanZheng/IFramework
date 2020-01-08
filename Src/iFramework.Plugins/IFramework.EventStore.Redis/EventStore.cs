using System.Linq;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IFramework.EventStore.Redis
{
    public class EventStore : IEventStore
    {
        private readonly RedisEventStoreOptions _eventStoreOptions;
        private readonly ILogger<EventStore>    _logger;
        private readonly IMessageTypeProvider   _messageTypeProvider;
        private          ConnectionMultiplexer  _connectionMultiplexer;
        private          IDatabase              _db;
        private          LuaScript              _luaScript;

        public EventStore(IOptions<RedisEventStoreOptions> eventStoreOptions, IMessageTypeProvider messageTypeProvider, ILogger<EventStore> logger)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _messageTypeProvider = messageTypeProvider;
            _logger = logger;
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
                    local aggregateCommandKey = string.format('ag:{%s}:commands', @aggregateId)
                    local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId)
                    if redis.call('SISMEMBER', aggregateCommandKey, @commandId) == '1' then
                        return -1
                    end
                    local expectedVersion = redis.call('ZREVRANGE', aggregateEventKey, '0', '0', 'WITHSCORES')
                    if #expectedVersion == 0 or expectedVersion[2] == @expectedVersion then
                        local version = @expectedVersion + 1
                        redis.call('SADD', aggregateCommandKey, @commandId)     
                        redis.call('ZADD', aggregateEventKey, version, @events)
                        return version
                    else
                        return -2
                    end
                ";
            }

            _luaScript = LuaScript.Prepare(luaScript);
        }

        public async Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            var agId = $"ag:{{{id}}}:events";
            var results = await _db.SortedSetRangeByScoreWithScoresAsync(agId,
                                                                         start,
                                                                         end ?? double.PositiveInfinity)
                                   .ConfigureAwait(false);
            return results?.SelectMany(r => r.Element
                                                        .ToString()
                                                        .ToJsonObject<EventPayload[]>()
                                                        .Select(ep => ep.Payload
                                                             .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent))
                          .ToArray();
        }

        public async Task AppendEvents(string aggregateId, long expectedVersion, string correlationId, params IEvent[] events)
        {
            var redisResult = await _db.ScriptEvaluateAsync(_luaScript,
                                                            new {
                                                                aggregateId = (RedisKey) aggregateId,
                                                                commandId = correlationId,
                                                                expectedVersion,
                                                                events = events.Select(e => new EventPayload {
                                                                                   Code = _messageTypeProvider.GetMessageCode(e.GetType()),
                                                                                   Payload = e.ToJson()
                                                                               })
                                                                               .ToJson()
                                                            })
                                       .ConfigureAwait(false);

            _logger.LogDebug(redisResult);
        }
    }
}