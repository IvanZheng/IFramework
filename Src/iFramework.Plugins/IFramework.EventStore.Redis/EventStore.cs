using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
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
        private readonly ILogger<EventStore> _logger;
        private readonly IMessageTypeProvider _messageTypeProvider;
        private LuaScript _appendEventsLuaScript;
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private LuaScript _getEventsLuaScript;
        private LuaScript _handleEventLuaScript;

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
                    local aggregateCommandKey = string.format('ag:{%s}:%s', @aggregateId, @commandId)
                    local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId)
                    
                    local version = redis.call('JSON.GET', aggregateCommandKey, '.version')
                    if version then
                        return redis.call('JSON.GET', aggregateCommandKey, '.result')
                    end
                    local expectedVersion = redis.call('ZREVRANGE', aggregateEventKey, '0', '0', 'WITHSCORES')
                    if #expectedVersion == 0 or expectedVersion[2] == @expectedVersion then
                        local newVersion = @expectedVersion + 1
                        redis.call('JSON.SET', aggregateCommandKey, '.', @result)     
                        redis.call('ZADD', aggregateEventKey, newVersion, @events)
                        return newVersion
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
                local aggregateCommandKey = string.format('ag:{%s}:%s', @aggregateId, @commandId)
                local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId) 
                local version = redis.call('JSON.GET', aggregateCommandKey, '.version')
                return redis.call('ZRANGEBYSCORE',aggregateEventKey, version, version)
                ";
            }

            _getEventsLuaScript = LuaScript.Prepare(getEventsLuaScript);

            var handleEventLuaScript = _eventStoreOptions.HandleEventLuaScript;
            if (string.IsNullOrWhiteSpace(handleEventLuaScript))
            {
                handleEventLuaScript = @"
                local result = redis.call('HSETNX', @subscriber, @eventId, @commands)
                if result == 1 then
                    return 1                
                else
                    return redis.call('HGET', @subscriber, @eventId)
                end
                ";
            }

            _handleEventLuaScript = LuaScript.Prepare(handleEventLuaScript);
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
                                             .ToJsonObject<ObjectPayload[]>()
                                             .Select(ep => ep.Payload
                                                             .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent))
                          .ToArray();
        }

        public async Task AppendEvents(string aggregateId, long expectedVersion, string correlationId, object commandResult, params IEvent[] events)
        {
            var eventsBody = events.Select(e => new ObjectPayload(e, _messageTypeProvider.GetMessageCode(e.GetType())))
                                   .ToJson();
            var redisResult = await _db.ScriptEvaluateAsync(_appendEventsLuaScript,
                                                            new {
                                                                aggregateId = (RedisKey) aggregateId,
                                                                commandId = correlationId,
                                                                expectedVersion,
                                                                result = new {
                                                                    version = expectedVersion + 1,
                                                                    result = commandResult != null ? new ObjectPayload(commandResult, _messageTypeProvider.GetMessageCode(commandResult.GetType()).ToJson()) : new ObjectPayload()
                                                                }.ToJson(),
                                                                events = eventsBody
                                                            })
                                       .ConfigureAwait(false);
            _logger.LogDebug($"redisResult:{redisResult} aggregateId:{aggregateId} expectedVersion:{expectedVersion} correlationId:{correlationId} events:{eventsBody}");
            if (redisResult.Type == ResultType.BulkString || redisResult.Type == ResultType.SimpleString)
            {
                var objectPayload = ((string) redisResult).ToJsonObject<ObjectPayload>();
                if (!string.IsNullOrWhiteSpace(objectPayload?.Payload))
                {
                    commandResult = objectPayload.Payload
                                                 .ToJsonObject(_messageTypeProvider.GetMessageType(objectPayload.Code));
                }

                events = await GetEvents(aggregateId, correlationId).ConfigureAwait(false);
                throw new MessageDuplicatelyHandled(commandResult, events);
            }

            if ((int) redisResult == -2)
            {
                throw new DBConcurrencyException($"aggregateId:{aggregateId} expectedVersion:{expectedVersion} concurrency conflict");
            }
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
                         .ToJsonObject<ObjectPayload[]>()
                         .Select(ep => ep.Payload
                                         .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                         .ToArray();
            return events;
        }

        public async Task<ICommand[]> HandleEvent(string subscriber, string eventId, ICommand[] commands)
        {
            var commandsBody = commands.Select(c => new ObjectPayload(c, _messageTypeProvider.GetMessageCode(c.GetType())))
                                       .ToJson();
            var redisResult = await _db.ScriptEvaluateAsync(_handleEventLuaScript, new {
                                           subscriber = (RedisKey) subscriber,
                                           eventId,
                                           commands = commandsBody
                                       })
                                       .ConfigureAwait(false);
            if (redisResult.Type == ResultType.SimpleString || redisResult.Type == ResultType.BulkString)
            {
                commands = ((string) redisResult)
                           .ToJsonObject<ObjectPayload[]>()
                           .Select(ep => ep.Payload
                                           .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as ICommand)
                           .ToArray();
            }

            return commands;
        }
    }
}