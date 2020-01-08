using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Event;
using IFramework.Exceptions;
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
        private          LuaScript              _appendEventsLuaScript;
        private          ConnectionMultiplexer  _connectionMultiplexer;
        private          IDatabase              _db;
        private          LuaScript              _getEventsLuaScript;

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

            var appendEventsLuaScript = _eventStoreOptions.AppendEventsLuaScript;
            if (string.IsNullOrWhiteSpace(appendEventsLuaScript))
            {
                appendEventsLuaScript = @"
                    local aggregateCommandKey = string.format('ag:{%s}:commands', @aggregateId)
                    local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId)

                    if redis.call('HEXISTS', aggregateCommandKey, @commandId) == '1' then
                        return -1
                    end
                    local expectedVersion = redis.call('ZREVRANGE', aggregateEventKey, '0', '0', 'WITHSCORES')
                    if #expectedVersion == 0 or expectedVersion[2] == @expectedVersion then
                        local version = @expectedVersion + 1
                        redis.call('HMSET', aggregateCommandKey, @commandId, version)     
                        redis.call('ZADD', aggregateEventKey, version, @events)
                        return version
                    else
                        return -2
                    end
                ";
            }

            _appendEventsLuaScript = LuaScript.Prepare(appendEventsLuaScript);

            var getEventsLuaScript = _eventStoreOptions.GetEventsLuaScript;
            if (string.IsNullOrWhiteSpace(getEventsLuaScript))
            {
                getEventsLuaScript = @"
                local aggregateCommandKey = string.format('ag:{%s}:commands', @aggregateId)
                local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId) 
                local version =  redis.call('HGET', aggregateCommandKey, @commandId)
                return redis.call('ZRANGEBYSCORE',aggregateEventKey, version, version)
                ";
            }

            _getEventsLuaScript = LuaScript.Prepare(getEventsLuaScript);
        }

        public async Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            var agEventKey = $"ag:{{{id}}}:events";
            var results = await _db.SortedSetRangeByScoreWithScoresAsync(agEventKey,
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
            var eventsBody = events.Select(e => new EventPayload {
                                       Code = _messageTypeProvider.GetMessageCode(e.GetType()),
                                       Payload = e.ToJson()
                                   })
                                   .ToJson();
            var redisResult = await _db.ScriptEvaluateAsync(_appendEventsLuaScript,
                                                            new {
                                                                aggregateId = (RedisKey) aggregateId,
                                                                commandId = correlationId,
                                                                expectedVersion,
                                                                events = eventsBody
                                                            })
                                       .ConfigureAwait(false);
            if ((int) redisResult == -1)
            {
                throw new MessageDuplicatelyHandled();
            }

            if ((int) redisResult == -2)
            {
                throw new DBConcurrencyException($"aggregateId:{aggregateId} expectedVersion:{expectedVersion} concurrency conflict");
            }

            _logger.LogDebug($"redisResult:{redisResult} aggregateId:{aggregateId} expectedVersion:{expectedVersion} correlationId:{correlationId} events:{eventsBody}");
        }

        public async Task<IEvent[]> GetEvents(string id, string commandId)
        {
            var redisResult = await _db.ScriptEvaluateAsync(_getEventsLuaScript,
                                                            new {
                                                                aggregateId = (RedisKey) id,
                                                                commandId
                                                            })
                                       .ConfigureAwait(false);
            var events = ((string) redisResult)
                         .ToJsonObject<EventPayload[]>()
                         .Select(ep => ep.Payload
                                         .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                         .ToArray();
            return events;
        }
    }
}